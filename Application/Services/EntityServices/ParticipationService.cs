using Application.Exceptions;
using Application.Extensions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Notifications;
using Application.Interfaces.Payments;
using Application.Interfaces.Repositories;
using Application.Models.Requests.Abstractions;
using Application.Models.Results;
using Application.Models.ViewModels.Participation;
using Application.Notifications;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Application.Services.EntityServices
{
    public class ParticipationService(
        IAuctionParticipantRepository participants,
        IAuctionRepository auctions,
        IPropertyRepository properties,
        IUserRepository users,
        IAuctionAwardRepository awards,
        IAuctionResultRepository results,
        IEimzoService eimzoService,
        ICurrentUserService currentUser,
        IPermissionService permissionService,
        INotificationService notifications,
        IDepositRefundService refunds,
        IMapper mapper,
        IUnitOfWork unitOfWork) : IParticipationService
    {
        // ---------------- the applicant

        public async Task<ResponseModel<ApplicationTextViewModel>> GetApplicationTextAsync(Guid auctionId, CancellationToken cancellationToken = default)
        {
            var (user, auction) = await LoadApplicantAsync(auctionId, cancellationToken);
            var text = await BuildTextAsync(auction, user, cancellationToken);
            return ResponseModel<ApplicationTextViewModel>.Ok(new ApplicationTextViewModel(text, Convert.ToBase64String(Encoding.UTF8.GetBytes(text)), auction.ApplicationDeadline));
        }

        /// <summary>
        /// The signature is checked in four steps: it gets a trusted timestamp, it verifies, the signed text is exactly the
        /// application text of this auction and this applicant, and the certificate belongs to the applicant's own PINFL/INN.
        /// The timestamped PKCS#7 is stored as the evidence.
        /// </summary>
        public Task<ResponseModel<ParticipantViewModel>> ApplyAsync(Guid auctionId, string signedData, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var (user, auction) = await LoadApplicantAsync(auctionId, cancellationToken);
                await EnsureCanApplyAsync(user, auction, cancellationToken);
                var expected = await BuildTextAsync(auction, user, cancellationToken);

                var timestamped = await eimzoService.TimestampAsync(signedData, cancellationToken);
                var verification = await eimzoService.VerifyAttachedAsync(timestamped, cancellationToken);
                if (!verification.IsSuccess)
                    throw new BadRequestException($"E-IMZO: {verification.Message}");
                if (verification.Signers.Count == 0 || verification.Signers.Any(s => !s.SignatureVerified || !s.CertificateVerified))
                    throw new BadRequestException("E-IMZO: the signature or the certificate is not valid.");
                EnsureSameText(verification.DocumentBase64, expected);
                var signer = SignerIdentifier(verification.Signers.Select(s => s.Certificate), user);

                return ResponseModel<ParticipantViewModel>.Ok(await CreateParticipantAsync(user, auction, timestamped, signer, cancellationToken));
            }, cancellationToken);

        public Task<ResponseModel<ParticipantViewModel>> ApplyMobileAsync(Guid auctionId, string documentId, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var (user, auction) = await LoadApplicantAsync(auctionId, cancellationToken);
                await EnsureCanApplyAsync(user, auction, cancellationToken);
                var expected = await BuildTextAsync(auction, user, cancellationToken);

                // The server checks the finished phone session against exactly this text; a different document fails its digest.
                var verification = await eimzoService.VerifyMobileAsync(documentId, Convert.ToBase64String(Encoding.UTF8.GetBytes(expected)), cancellationToken);
                if (!verification.IsSuccess)
                    throw new BadRequestException($"E-IMZO: {verification.Message}");
                var signer = SignerIdentifier([verification.Certificate], user);

                return ResponseModel<ParticipantViewModel>.Ok(await CreateParticipantAsync(user, auction, verification.Pkcs7Attached ?? documentId, signer, cancellationToken));
            }, cancellationToken);

        public Task<ResponseModel<ParticipantViewModel>> WithdrawAsync(Guid auctionId, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
                var participant = await participants.GetAsync(p => p.AuctionId == auctionId && p.UserId == userId && p.Status != ParticipantStatus.WITHDRAWN, cancellationToken)
                                  ?? throw new NotFoundException("You have no application for this auction.");
                var auction = await auctions.GetAsync(a => a.Id == auctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");

                if (participant.Status == ParticipantStatus.REJECTED)
                    throw new ConflictException("The application was rejected.");
                // Once the sale has started, an application is a commitment (the leader especially).
                if (auction.Status != AuctionStatus.SCHEDULED || DateTime.UtcNow >= auction.StartsAt)
                    throw new ConflictException("The sale has started; the application can no longer be withdrawn.");

                participant.Status = ParticipantStatus.WITHDRAWN;
                await participants.UpdateAsync(participant, cancellationToken);
                await refunds.RequestAsync(participant, auction, "Ariza qaytarib olindi", cancellationToken);
                return ResponseModel<ParticipantViewModel>.Ok(mapper.Map<ParticipantViewModel>(participant));
            }, cancellationToken);

        public async Task<ResponseModel<List<MyParticipationViewModel>>> GetMyAsync(CancellationToken cancellationToken = default)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            var mine = await participants.Query(p => p.UserId == userId, cancellationToken)
                                         .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).ToListAsync(cancellationToken);
            var auctionIds = mine.Select(p => p.AuctionId).Distinct().ToList();
            var byAuction = await auctions.Query(a => auctionIds.Contains(a.Id), cancellationToken).ToDictionaryAsync(a => a.Id, cancellationToken);
            var myIds = mine.Select(p => p.Id).ToList();
            var myAwards = (await awards.Query(a => myIds.Contains(a.ParticipantId), cancellationToken).ToListAsync(cancellationToken))
                           .GroupBy(a => a.ParticipantId).ToDictionary(g => g.Key, g => g.OrderBy(a => a.Rank).Last());

            var items = mine.Where(p => byAuction.ContainsKey(p.AuctionId)).Select(p =>
            {
                var auction = byAuction[p.AuctionId];
                var award = myAwards.GetValueOrDefault(p.Id);
                return new MyParticipationViewModel
                {
                    Id = p.Id, AuctionId = auction.Id, LotNumber = auction.LotNumber, AuctionTitle = auction.Title,
                    AuctionStatus = auction.Status, StartsAt = auction.StartsAt, EndsAt = auction.EndsAt, DepositAmount = auction.DepositAmount,
                    Status = p.Status, ParticipantNumber = p.ParticipantNumber, DepositStatus = p.DepositStatus, RejectReason = p.RejectReason,
                    CreatedAt = p.CreatedAt,
                    Award = award == null ? null : new MyAwardViewModel
                    {
                        Rank = award.Rank, Status = award.Status, Price = award.Price,
                        AmountDue = Math.Max(0, award.Price - (p.DepositStatus == DepositStatus.PAID ? auction.DepositAmount : 0)),
                        RespondBy = award.RespondBy, PaymentDueAt = award.PaymentDueAt,
                    },
                };
            }).ToList();
            return ResponseModel<List<MyParticipationViewModel>>.Ok(items);
        }

        // ---------------- staff

        public async Task<ResponseModel<List<ParticipantAdminViewModel>>> GetListAsync(BaseGetListQuery query, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync("READ_AUCTIONPARTICIPANT");
            var filtered = participants.Query(query.BuildPredicate<AuctionParticipant>(), cancellationToken);
            var total = await filtered.CountAsync(cancellationToken);
            var ordered = query.BuildOrderBy<AuctionParticipant>() is { } orderBy
                ? orderBy(filtered)
                : filtered.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id);
            var page = await ordered.Include(p => p.User).Skip(query.PageIndex * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
            return ResponseModel<List<ParticipantAdminViewModel>>.Ok(page.Select(ToAdminView).ToList(), query.PageIndex, query.PageSize, total);
        }

        public Task<ResponseModel<ParticipantAdminViewModel>> ApproveAsync(Guid participantId, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync("UPDATE_AUCTIONPARTICIPANT");
                var (participant, auction) = await LoadForReviewAsync(participantId, cancellationToken);
                if (participant.Status != ParticipantStatus.PENDING)
                    throw new ConflictException($"The application is already {participant.Status}.");

                // Numbers 1, 2, 3... in the order of approval; the unique index (auction, number) stops two approvals taking the same one.
                var last = await participants.Query(p => p.AuctionId == auction.Id && p.ParticipantNumber != null, cancellationToken)
                                             .MaxAsync(p => p.ParticipantNumber, cancellationToken) ?? 0;
                participant.Status = ParticipantStatus.APPROVED;
                participant.ParticipantNumber = last + 1;
                participant.DepositStatus = auction.DepositAmount > 0 ? DepositStatus.NOT_PAID : DepositStatus.PAID;
                Review(participant);
                await participants.UpdateAsync(participant, cancellationToken);

                await notifications.NotifyAsync(participant.UserId, NotificationType.APPLICATION_APPROVED, new Dictionary<string, string>
                {
                    ["lot"] = auction.LotNumber,
                    ["number"] = participant.ParticipantNumber.ToString()!,
                    ["deposit"] = NotificationTemplates.Money(auction.DepositAmount),
                    ["start"] = NotificationTemplates.Time(auction.StartsAt),
                }, auction.Id, cancellationToken: cancellationToken);
                return ResponseModel<ParticipantAdminViewModel>.Ok(await AdminViewAsync(participant.Id, cancellationToken));
            }, cancellationToken);

        public Task<ResponseModel<ParticipantAdminViewModel>> RejectAsync(Guid participantId, string reason, CancellationToken cancellationToken = default)
            => unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync("UPDATE_AUCTIONPARTICIPANT");
                var (participant, auction) = await LoadForReviewAsync(participantId, cancellationToken);
                if (participant.Status is not (ParticipantStatus.PENDING or ParticipantStatus.APPROVED))
                    throw new ConflictException($"The application is already {participant.Status}.");

                participant.Status = ParticipantStatus.REJECTED;
                participant.RejectReason = reason.Trim();
                Review(participant);
                await participants.UpdateAsync(participant, cancellationToken);
                await refunds.RequestAsync(participant, auction, "Ariza rad etildi", cancellationToken);

                await notifications.NotifyAsync(participant.UserId, NotificationType.APPLICATION_REJECTED, new Dictionary<string, string>
                {
                    ["lot"] = auction.LotNumber,
                    ["reason"] = participant.RejectReason,
                }, auction.Id, cancellationToken: cancellationToken);
                return ResponseModel<ParticipantAdminViewModel>.Ok(await AdminViewAsync(participant.Id, cancellationToken));
            }, cancellationToken);

        // ---------------- helpers

        private void Review(AuctionParticipant participant)
        {
            participant.ReviewedAt = DateTime.UtcNow;
            participant.ReviewedBy = currentUser.UserId;
        }

        private async Task EnsurePermissionAsync(string permission)
        {
            if (!await permissionService.Check(permission))
                throw new ForbiddenException("You do not have permission for this operation.");
        }

        /// <summary>An application can be reviewed while the sale has not started.</summary>
        private async Task<(AuctionParticipant Participant, Auction Auction)> LoadForReviewAsync(Guid participantId, CancellationToken cancellationToken)
        {
            var participant = await participants.GetAsync(p => p.Id == participantId, cancellationToken) ?? throw new NotFoundException("Application not found");
            var auction = await auctions.GetAsync(a => a.Id == participant.AuctionId, cancellationToken) ?? throw new NotFoundException("Auction not found");
            if (auction.Status != AuctionStatus.SCHEDULED || DateTime.UtcNow >= auction.StartsAt)
                throw new ConflictException("The sale has started or the auction is not open; applications can no longer be reviewed.");
            return (participant, auction);
        }

        private async Task<ParticipantAdminViewModel> AdminViewAsync(Guid participantId, CancellationToken cancellationToken)
            => ToAdminView(await participants.Query(p => p.Id == participantId, cancellationToken).Include(p => p.User).FirstAsync(cancellationToken));

        private ParticipantAdminViewModel ToAdminView(AuctionParticipant p)
        {
            var view = mapper.Map<ParticipantAdminViewModel>(p);
            view.UserFullName = p.User?.FullName ?? string.Empty;
            view.UserIdentifier = p.User?.INNPINFL;
            view.UserPhone = p.User?.Phone;
            view.UserEmail = p.User?.Email;
            return view;
        }

        /// <summary>The signed-in applicant and the auction they apply to (a draft is not visible).</summary>
        private async Task<(User User, Auction Auction)> LoadApplicantAsync(Guid auctionId, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? throw new UnauthenticatedException("Sign in first.");
            var user = await users.GetAsync(u => u.Id == userId, cancellationToken) ?? throw new UnauthenticatedException("Sign in first.");
            var auction = await auctions.GetAsync(a => a.Id == auctionId && a.Status != AuctionStatus.DRAFT, cancellationToken)
                          ?? throw new NotFoundException("Auction not found");
            // Only an account whose person or organisation was proved by an E-IMZO key may bid (T-12, P2-13).
            if (user.Type == ClientType.NONE || string.IsNullOrWhiteSpace(user.INNPINFL))
                throw new ForbiddenException("Link your E-IMZO key to your account first (POST api/auth/me/eimzo).");
            return (user, auction);
        }

        private async Task EnsureCanApplyAsync(User user, Auction auction, CancellationToken cancellationToken)
        {
            if (auction.Status != AuctionStatus.SCHEDULED || DateTime.UtcNow > auction.ApplicationDeadline)
                throw new ConflictException("Applications for this auction are closed.");
            if (await participants.AnyAsync(p => p.AuctionId == auction.Id && p.UserId == user.Id && p.Status != ParticipantStatus.WITHDRAWN, cancellationToken))
                throw new ConflictException("You have already applied to this auction.");
        }

        /// <summary>
        /// Fixed wording: the signature is only worth something if both sides know exactly what was signed.
        /// It names the lot, the property, the terms and the applicant's PINFL/INN, so it cannot be reused for another auction or person.
        /// </summary>
        private async Task<string> BuildTextAsync(Auction auction, User user, CancellationToken cancellationToken)
        {
            var property = await properties.GetAsync(p => p.Id == auction.PropertyId, cancellationToken);
            var idLabel = user.Type == ClientType.LEGAL ? "INN" : "PINFL";
            return string.Join('\n',
                "I-Rieltor.uz auksionida qatnashish arizasi",
                "",
                $"Lot: {auction.LotNumber}",
                $"Obyekt: {property?.Title ?? auction.Title}",
                $"Boshlang'ich narx: {NotificationTemplates.Money(auction.StartingPrice)} so'm",
                $"Zakalat: {NotificationTemplates.Money(auction.DepositAmount)} so'm",
                $"Ariza beruvchi {idLabel}: {user.INNPINFL}",
                "",
                "Men auksion shartlari bilan tanishdim va ularga roziman.");
        }

        private static void EnsureSameText(string? signedDocumentBase64, string expected)
        {
            string signed;
            try
            {
                signed = Encoding.UTF8.GetString(Convert.FromBase64String(signedDocumentBase64 ?? string.Empty));
            }
            catch (FormatException)
            {
                throw new BadRequestException("The signed document cannot be read.");
            }
            if (!string.Equals(signed.ReplaceLineEndings("\n"), expected, StringComparison.Ordinal))
                throw new BadRequestException("The signed text is not this auction's application text. Request the text again and sign it.");
        }

        /// <summary>
        /// The key that signed must be the applicant's own: its PINFL (personal key) or organisation INN (legal-entity key)
        /// equals the one on the account. Returns that identifier.
        /// </summary>
        private static string SignerIdentifier(IEnumerable<Application.Models.Results.EimzoCertificateInfo?> certificates, User user)
        {
            var identifiers = certificates.Where(c => c != null)
                                          .Select(c => c!.IsLegalEntity ? c.LegalInn : c.Pinfl)
                                          .Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            if (!identifiers.Contains(user.INNPINFL))
                throw new ForbiddenException("The application was signed with a key that is not linked to your account.");
            return user.INNPINFL!;
        }

        private async Task<ParticipantViewModel> CreateParticipantAsync(User user, Auction auction, string evidence, string signer, CancellationToken cancellationToken)
        {
            var created = await participants.CreateAsync(new AuctionParticipant
            {
                AuctionId = auction.Id,
                UserId = user.Id,
                Status = ParticipantStatus.PENDING,
                SignedApplication = evidence,
                SignerIdentifier = signer,
                SignedAt = DateTime.UtcNow,
            }, cancellationToken);
            return mapper.Map<ParticipantViewModel>(created);
        }
    }
}
