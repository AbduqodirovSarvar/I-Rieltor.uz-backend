using Application.Features.Abstractions.Commands.Interfaces;
using Application.Features.Abstractions.Queries.Interfaces;
using Application.Interfaces.Auths;
using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Exceptions;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Payments;
using Domain.Behaviours;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.Payments
{
    public record CheckoutCommand : ICommand<ResponseModel<CheckoutViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }

        /// <summary>DEPOSIT or FINAL_PAYMENT.</summary>
        public PaymentPurpose Purpose { get; init; }

        /// <summary>PAYME, CLICK, BANK_TRANSFER or CARD_TRANSFER.</summary>
        public PaymentProvider Provider { get; init; }
    }

    public record GetMyPaymentsQuery : BaseGetListQuery, IQuery<ResponseModel<List<PaymentViewModel>>>;

    /// <summary>The payment methods that are switched on, so the site offers only those.</summary>
    public record GetPaymentProvidersQuery : IQuery<ResponseModel<List<PaymentProvider>>>;

    public record GetPaymentListQuery : BaseGetListQuery, IQuery<ResponseModel<List<PaymentViewModel>>>;

    public record RecordManualPaymentCommand : ICommand<ResponseModel<PaymentViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid ParticipantId { get; init; }
        public PaymentPurpose Purpose { get; init; }
        [Range(0.01, 1_000_000_000_000_000.0)]
        public decimal Amount { get; init; }
        /// <summary>BANK_TRANSFER, CARD_TRANSFER or MANUAL.</summary>
        public PaymentProvider Provider { get; init; } = PaymentProvider.BANK_TRANSFER;
        /// <summary>The bank's reference of the transfer.</summary>
        [MaxLength(100)]
        public string? Reference { get; init; }
        [MaxLength(500)]
        public string? Note { get; init; }
    }

    public record ConfirmPaymentCommand : ICommand<ResponseModel<PaymentViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid Id { get; init; }
        [MaxLength(100)]
        public string? Reference { get; init; }
    }

    public record CompleteRefundCommand : ICommand<ResponseModel<PaymentViewModel>>
    {
        [Required, NotEmptyGuid]
        public Guid Id { get; init; }
        [MaxLength(100)]
        public string? Reference { get; init; }
    }

    /// <summary>The runner-up takes the property at their own highest bid.</summary>
    public record AcceptOfferCommand : ICommand<ResponseModel<bool>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
    }

    /// <summary>The runner-up declines; their deposit goes back.</summary>
    public record DeclineOfferCommand : ICommand<ResponseModel<bool>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
    }

    /// <summary>The buyer gives up: the deposit is forfeited and the property goes to the runner-up (if the auction offers it).</summary>
    public record RefuseAwardCommand : ICommand<ResponseModel<bool>>
    {
        [Required, NotEmptyGuid]
        public Guid AuctionId { get; init; }
    }

    public class PaymentRequestHandlers(IPaymentService service, IAuctionSettlement settlement, ICurrentUserService currentUser, IUnitOfWork unitOfWork)
        : ICommandHandler<CheckoutCommand, ResponseModel<CheckoutViewModel>>,
          IQueryHandler<GetMyPaymentsQuery, ResponseModel<List<PaymentViewModel>>>,
          IQueryHandler<GetPaymentProvidersQuery, ResponseModel<List<PaymentProvider>>>,
          IQueryHandler<GetPaymentListQuery, ResponseModel<List<PaymentViewModel>>>,
          ICommandHandler<RecordManualPaymentCommand, ResponseModel<PaymentViewModel>>,
          ICommandHandler<ConfirmPaymentCommand, ResponseModel<PaymentViewModel>>,
          ICommandHandler<CompleteRefundCommand, ResponseModel<PaymentViewModel>>,
          ICommandHandler<AcceptOfferCommand, ResponseModel<bool>>,
          ICommandHandler<DeclineOfferCommand, ResponseModel<bool>>,
          ICommandHandler<RefuseAwardCommand, ResponseModel<bool>>
    {
        public Task<ResponseModel<CheckoutViewModel>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
            => service.CheckoutAsync(request.AuctionId, request.Purpose, request.Provider, cancellationToken);

        public Task<ResponseModel<List<PaymentViewModel>>> Handle(GetMyPaymentsQuery request, CancellationToken cancellationToken)
            => service.GetMyAsync(request, cancellationToken);

        public Task<ResponseModel<List<PaymentProvider>>> Handle(GetPaymentProvidersQuery request, CancellationToken cancellationToken)
            => Task.FromResult(ResponseModel<List<PaymentProvider>>.Ok(service.GetAvailableProviders()));

        public Task<ResponseModel<List<PaymentViewModel>>> Handle(GetPaymentListQuery request, CancellationToken cancellationToken)
            => service.GetListAsync(request, cancellationToken);

        public Task<ResponseModel<PaymentViewModel>> Handle(RecordManualPaymentCommand request, CancellationToken cancellationToken)
            => service.RecordManualAsync(request.ParticipantId, request.Purpose, request.Amount, request.Provider, request.Reference, request.Note, cancellationToken);

        public Task<ResponseModel<PaymentViewModel>> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
            => service.ConfirmAsync(request.Id, request.Reference, cancellationToken);

        public Task<ResponseModel<PaymentViewModel>> Handle(CompleteRefundCommand request, CancellationToken cancellationToken)
            => service.CompleteRefundAsync(request.Id, request.Reference, cancellationToken);

        public Task<ResponseModel<bool>> Handle(AcceptOfferCommand request, CancellationToken cancellationToken)
            => Answer(userId => settlement.AcceptOfferAsync(userId, request.AuctionId, DateTime.UtcNow, cancellationToken), cancellationToken);

        public Task<ResponseModel<bool>> Handle(DeclineOfferCommand request, CancellationToken cancellationToken)
            => Answer(userId => settlement.DeclineOfferAsync(userId, request.AuctionId, DateTime.UtcNow, cancellationToken), cancellationToken);

        public Task<ResponseModel<bool>> Handle(RefuseAwardCommand request, CancellationToken cancellationToken)
            => Answer(userId => settlement.RefuseAsync(userId, request.AuctionId, DateTime.UtcNow, cancellationToken), cancellationToken);

        /// <summary>A participant's answer changes the award, the result, the money and sends notifications: one transaction.</summary>
        private Task<ResponseModel<bool>> Answer(Func<Guid, Task> action, CancellationToken cancellationToken)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await action(currentUser.UserId ?? throw new UnauthenticatedException("Sign in first."));
                return ResponseModel<bool>.Ok(true);
            }, cancellationToken);
    }
}
