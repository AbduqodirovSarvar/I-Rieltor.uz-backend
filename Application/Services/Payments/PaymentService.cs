using Application.Exceptions;
using Application.Extentions;
using Application.Interfaces.Auths;
using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Models.Configuration;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Payments;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Application.Services.Payments
{
    public sealed class PaymentService(
        IPaymentRepository payments,
        IAuctionRepository auctions,
        IAuctionParticipantRepository participants,
        IAuctionAwardRepository awards,
        IPaymentProcessor processor,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        PaymentSettings settings,
        IMapper mapper,
        IUnitOfWork unitOfWork) : IPaymentService
    {
        // ---------------- the payer

        public Task<ResponseModel<CheckoutViewModel>> CheckoutAsync(Guid auctionId, PaymentPurpose purpose, PaymentProvider provider, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
                var auction = await auctions.GetAsync(a => a.Id == auctionId && a.Status != AuctionStatus.DRAFT, cancellationToken)
                              ?? throw new NotFoundException("Auction not found");
                var participant = await participants.GetAsync(p => p.AuctionId == auctionId && p.UserId == userId && p.Status == ParticipantStatus.APPROVED, cancellationToken)
                                  ?? throw new ForbiddenException("You are not an approved participant of this auction.");
                EnsureProviderAvailable(provider);

                decimal amount;
                var now = DateTime.UtcNow;
                switch (purpose)
                {
                    case PaymentPurpose.DEPOSIT:
                        if (auction.DepositAmount <= 0)
                            throw new ConflictException("This auction has no deposit.");
                        if (participant.DepositStatus != DepositStatus.NOT_PAID)
                            throw new ConflictException("The deposit is already paid.");
                        if (auction.Status is not (AuctionStatus.SCHEDULED or AuctionStatus.ONGOING))
                            throw new ConflictException("The auction no longer takes deposits.");
                        amount = auction.DepositAmount;
                        break;
                    case PaymentPurpose.FINAL_PAYMENT:
                        var award = await awards.GetAsync(a => a.ParticipantId == participant.Id && a.Status == AwardStatus.ACCEPTED && a.AuctionResult!.AuctionId == auctionId, cancellationToken)
                                    ?? throw new ConflictException("You have no purchase waiting for payment in this auction.");
                        if (award.PaymentDueAt <= now)
                            throw new ConflictException("The payment deadline has passed.");
                        amount = AuctionSettlement.AmountDue(award, auction, participant);
                        break;
                    default:
                        throw new BadRequestException("Only a deposit or the final payment can be started here.");
                }

                // The same payment asked for again is the same payment: a second tab or a retry does not pile up payments.
                var existing = await payments.GetAsync(p => p.ParticipantId == participant.Id && p.Purpose == purpose && p.Provider == provider
                                                            && p.Status == PaymentStatus.PENDING && p.Amount == amount, cancellationToken);
                var payment = existing ?? await payments.CreateAsync(new Payment
                {
                    UserId = userId,
                    AuctionId = auctionId,
                    ParticipantId = participant.Id,
                    Purpose = purpose,
                    Amount = amount,
                    Status = PaymentStatus.PENDING,
                    Provider = provider,
                }, cancellationToken);
                return ResponseModel<CheckoutViewModel>.Ok(CheckoutOf(payment));
            }, cancellationToken);

        private void EnsureProviderAvailable(PaymentProvider provider)
        {
            var available = provider switch
            {
                PaymentProvider.PAYME => settings.Payme.Enabled,
                PaymentProvider.CLICK => settings.Click.Enabled,
                PaymentProvider.BANK_TRANSFER => settings.BankTransfer.Enabled,
                _ => false,
            };
            if (!available)
                throw new ConflictException($"Payment by {provider} is not available.");
        }

        private CheckoutViewModel CheckoutOf(Payment payment) => new()
        {
            PaymentId = payment.Id,
            Amount = payment.Amount,
            Purpose = payment.Purpose,
            Provider = payment.Provider,
            Url = payment.Provider switch
            {
                PaymentProvider.PAYME => PaymeUrl(payment),
                PaymentProvider.CLICK => ClickUrl(payment),
                _ => null,
            },
            Instructions = payment.Provider == PaymentProvider.BANK_TRANSFER
                ? $"{settings.BankTransfer.Instructions}\nTo'lov maqsadi: {payment.Id}"
                : null,
        };

        /// <summary>Payme's checkout page takes its parameters as one Base64 string: merchant, order, amount in tiyin, return address.</summary>
        private string PaymeUrl(Payment payment)
        {
            var parameters = $"m={settings.Payme.MerchantId};ac.order_id={payment.Id};a={(long)Math.Round(payment.Amount * 100)}";
            if (!string.IsNullOrWhiteSpace(settings.ReturnUrl))
                parameters += $";c={settings.ReturnUrl}";
            return $"{settings.Payme.CheckoutUrl.TrimEnd('/')}/{Convert.ToBase64String(Encoding.UTF8.GetBytes(parameters))}";
        }

        private string ClickUrl(Payment payment)
        {
            var url = $"{settings.Click.CheckoutUrl}?service_id={Uri.EscapeDataString(settings.Click.ServiceId)}&merchant_id={Uri.EscapeDataString(settings.Click.MerchantId)}"
                      + $"&amount={payment.Amount.ToString("0.00", CultureInfo.InvariantCulture)}&transaction_param={payment.Id}";
            return string.IsNullOrWhiteSpace(settings.ReturnUrl) ? url : $"{url}&return_url={Uri.EscapeDataString(settings.ReturnUrl)}";
        }

        public async Task<ResponseModel<List<PaymentViewModel>>> GetMyAsync(BaseGetListQuery query, CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            return await ListAsync(query, p => p.UserId == userId, cancellationToken);
        }

        public async Task<ResponseModel<List<PaymentViewModel>>> GetListAsync(BaseGetListQuery query, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync("READ_PAYMENT");
            return await ListAsync(query, null, cancellationToken);
        }

        private async Task<ResponseModel<List<PaymentViewModel>>> ListAsync(BaseGetListQuery query, System.Linq.Expressions.Expression<Func<Payment, bool>>? scope, CancellationToken cancellationToken)
        {
            var filter = query.BuildPredicate<Payment>();
            System.Linq.Expressions.Expression<Func<Payment, bool>>? predicate = (scope, filter) switch
            {
                (null, _) => filter,
                (_, null) => scope,
                _ => scope.AndAlso(filter),
            };
            var (data, total) = await payments.GetPaginatedAsync(predicate, query.PageIndex, query.PageSize,
                query.BuildOrderBy<Payment>() ?? (q => q.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)), cancellationToken);
            return ResponseModel<List<PaymentViewModel>>.Ok(mapper.Map<List<PaymentViewModel>>(data), query.PageIndex, query.PageSize, total);
        }

        // ---------------- staff

        public Task<ResponseModel<PaymentViewModel>> RecordManualAsync(Guid participantId, PaymentPurpose purpose, decimal amount, PaymentProvider provider, string? reference, string? note, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync("CREATE_PAYMENT");
                if (provider is not (PaymentProvider.BANK_TRANSFER or PaymentProvider.MANUAL))
                    throw new BadRequestException("Only bank transfers and manual entries are recorded by hand; Payme and Click report their own payments.");
                if (purpose is not (PaymentPurpose.DEPOSIT or PaymentPurpose.FINAL_PAYMENT))
                    throw new BadRequestException("Only a deposit or the final payment can be recorded; a refund is completed with its own request.");

                var participant = await participants.GetAsync(p => p.Id == participantId, cancellationToken) ?? throw new NotFoundException("Participant not found");
                var auction = await auctions.GetAsync(a => a.Id == participant.AuctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");
                var expected = purpose == PaymentPurpose.DEPOSIT
                    ? auction.DepositAmount
                    : AuctionSettlement.AmountDue(
                        await awards.GetAsync(a => a.ParticipantId == participant.Id && a.Status == AwardStatus.ACCEPTED && a.AuctionResult!.AuctionId == auction.Id, cancellationToken)
                            ?? throw new ConflictException("The participant has no purchase waiting for payment."), auction, participant);
                if (amount != expected)
                    throw new BadRequestException($"The amount must be exactly {Application.Notifications.NotificationTemplates.Money(expected)} so'm.");

                var payment = await payments.CreateAsync(new Payment
                {
                    UserId = participant.UserId,
                    AuctionId = auction.Id,
                    ParticipantId = participant.Id,
                    Purpose = purpose,
                    Amount = amount,
                    Status = PaymentStatus.PENDING,
                    Provider = provider,
                    ProviderTransactionId = Clean(reference),
                    Note = Clean(note),
                }, cancellationToken);
                await processor.MarkPaidAsync(payment, DateTime.UtcNow, cancellationToken);
                return ResponseModel<PaymentViewModel>.Ok(mapper.Map<PaymentViewModel>(payment));
            }, cancellationToken);

        public Task<ResponseModel<PaymentViewModel>> ConfirmAsync(Guid paymentId, string? reference, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync("UPDATE_PAYMENT");
                var payment = await payments.GetAsync(p => p.Id == paymentId, cancellationToken) ?? throw new NotFoundException("Payment not found");
                if (payment.Purpose == PaymentPurpose.REFUND)
                    throw new BadRequestException("A refund is completed with refund/complete.");
                if (payment.Provider is not (PaymentProvider.BANK_TRANSFER or PaymentProvider.MANUAL))
                    throw new BadRequestException("Payme and Click payments are confirmed by the provider, not by hand.");
                if (payment.Status != PaymentStatus.PENDING)
                    throw new ConflictException($"The payment is already {payment.Status}.");

                payment.ProviderTransactionId = Clean(reference) ?? payment.ProviderTransactionId;
                await processor.MarkPaidAsync(payment, DateTime.UtcNow, cancellationToken);
                return ResponseModel<PaymentViewModel>.Ok(mapper.Map<PaymentViewModel>(payment));
            }, cancellationToken);

        public Task<ResponseModel<PaymentViewModel>> CompleteRefundAsync(Guid paymentId, string? reference, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync("UPDATE_PAYMENT");
                var payment = await payments.GetAsync(p => p.Id == paymentId && p.Purpose == PaymentPurpose.REFUND, cancellationToken)
                              ?? throw new NotFoundException("Refund not found");
                if (payment.Status != PaymentStatus.PENDING)
                    throw new ConflictException($"The refund is already {payment.Status}.");

                payment.ProviderTransactionId = Clean(reference);
                await processor.MarkPaidAsync(payment, DateTime.UtcNow, cancellationToken);
                return ResponseModel<PaymentViewModel>.Ok(mapper.Map<PaymentViewModel>(payment));
            }, cancellationToken);

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private async Task EnsurePermissionAsync(string permission)
        {
            if (!await permissionService.Check(permission))
                throw new ForbiddenException("You do not have permission for this operation.");
        }
    }
}
