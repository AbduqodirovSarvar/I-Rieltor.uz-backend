using Application.Interfaces.Payments;
using Application.Interfaces.Auctions;
using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Auctions;
using Application.Models.Results;
using Application.Models.ViewModels.Auctions;
using Application.Models.ViewModels.Public;
using Application.Services.EntityServices.Abstractions;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services.EntityServices
{
    public class AuctionService(
        IAuctionRepository repository,
        IPermissionService permissionService,
        IMapper mapper,
        IPropertyRepository propertyRepository,
        IAuctionParticipantRepository participantRepository,
        IBidRepository bidRepository,
        IAuctionResultRepository resultRepository,
        IAuctionAwardRepository awardRepository,
        IAuctionEvents events,
        IAuctionSettlement settlement,
        IUnitOfWork unitOfWork)
        : GenericEntityService<Auction,
                               CreateAuctionDto,
                               UpdateAuctionDto,
                               AuctionViewModel>(repository, permissionService, mapper), IAuctionService
    {
        // The shortest sale that makes sense; shorter ones are mistakes (end typed before the start, minutes for hours...).
        private static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(5);

        private readonly IPropertyRepository _propertyRepository = propertyRepository;
        private readonly IAuctionParticipantRepository _participantRepository = participantRepository;
        private readonly IBidRepository _bidRepository = bidRepository;
        private readonly IAuctionResultRepository _resultRepository = resultRepository;
        private readonly IAuctionAwardRepository _awardRepository = awardRepository;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IAuctionEvents _events = events;
        private readonly IAuctionSettlement _settlement = settlement;

        public override Task<ResponseModel<AuctionViewModel>> CreateAsync(CreateAuctionDto dto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.CREATE);
                var property = await EnsureValidAsync(dto, null, cancellationToken);

                var entity = _mapper.Map<Auction>(dto);
                entity.Status = AuctionStatus.DRAFT;
                entity.Title = string.IsNullOrWhiteSpace(dto.Title) ? property.Title : dto.Title.Trim();
                entity.LotNumber = string.IsNullOrWhiteSpace(dto.LotNumber)
                    ? await NextLotNumberAsync(cancellationToken)
                    : dto.LotNumber.Trim();
                if (await _repository.AnyAsync(a => a.LotNumber == entity.LotNumber, cancellationToken))
                    throw new ConflictException($"Lot number {entity.LotNumber} is already used.");

                var result = await _repository.CreateAsync(entity, cancellationToken);
                return ResponseModel<AuctionViewModel>.Ok(_mapper.Map<AuctionViewModel>(result));
            }, cancellationToken);

        public override Task<ResponseModel<AuctionViewModel>> UpdateAsync(UpdateAuctionDto dto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                var entity = await _repository.GetAsync(a => a.Id == dto.Id, cancellationToken)
                             ?? throw new NotFoundException("Entity not found");
                if (entity.Status != AuctionStatus.DRAFT)
                    throw new ConflictException($"Only a DRAFT auction can be changed; this one is {entity.Status}. Cancel it and create a new one.");
                var property = await EnsureValidAsync(dto, entity.Id, cancellationToken);

                var lotNumber = entity.LotNumber;
                _mapper.Map(dto, entity);
                entity.Title = string.IsNullOrWhiteSpace(dto.Title) ? property.Title : dto.Title.Trim();
                // The lot number keeps its value unless a new one is given.
                entity.LotNumber = lotNumber;
                if (!string.IsNullOrWhiteSpace(dto.LotNumber) && dto.LotNumber.Trim() != lotNumber)
                {
                    if (await _repository.AnyAsync(a => a.LotNumber == dto.LotNumber.Trim() && a.Id != entity.Id, cancellationToken))
                        throw new ConflictException($"Lot number {dto.LotNumber.Trim()} is already used.");
                    entity.LotNumber = dto.LotNumber.Trim();
                }

                var result = await _repository.UpdateAsync(entity, cancellationToken);
                return ResponseModel<AuctionViewModel>.Ok(_mapper.Map<AuctionViewModel>(result));
            }, cancellationToken);

        public override async Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.DELETE);
            var entity = await _repository.GetAsync(a => a.Id == id, cancellationToken)
                         ?? throw new NotFoundException("Entity not found");
            if (entity.Status != AuctionStatus.DRAFT)
                throw new ConflictException($"Only a DRAFT auction can be deleted; this one is {entity.Status}. Cancel it instead.");
            return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
        }

        /// <summary>
        /// DRAFT → SCHEDULED: the auction is announced (applications open) and its property goes to IN_AUCTION.
        /// </summary>
        public Task<ResponseModel<AuctionViewModel>> PublishAsync(Guid id, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                var auction = await _repository.GetAsync(a => a.Id == id, cancellationToken)
                              ?? throw new NotFoundException("Entity not found");
                if (auction.Status != AuctionStatus.DRAFT)
                    throw new ConflictException($"The auction is already {auction.Status}.");

                var now = DateTime.UtcNow;
                if (auction.StartsAt <= now)
                    throw new BadRequestException("The start time has passed; change it before publishing.");
                if (auction.ApplicationDeadline <= now)
                    throw new BadRequestException("The application deadline has passed; change it before publishing.");

                var property = await _propertyRepository.GetAsync(p => p.Id == auction.PropertyId, cancellationToken)
                               ?? throw new BadRequestException("The property does not exist.");
                if (property.Status != PropertyStatus.PUBLISHED)
                    throw new BadRequestException($"The property is {property.Status}; publish it in the catalog first.");

                auction.Status = AuctionStatus.SCHEDULED;
                await _repository.UpdateAsync(auction, cancellationToken);
                property.Status = PropertyStatus.IN_AUCTION;
                await _propertyRepository.UpdateAsync(property, cancellationToken);
                await _unitOfWork.AfterCommitAsync(() => _events.StatusChangedAsync(new AuctionStatusChangedEvent(auction.Id, auction.Status, auction.EndsAt)));
                return ResponseModel<AuctionViewModel>.Ok(_mapper.Map<AuctionViewModel>(auction));
            }, cancellationToken);

        /// <summary>
        /// SCHEDULED or ONGOING → CANCELLED, with a reason shown to the participants. The property returns to the catalog.
        /// Deposits are refunded by the payment flow (P2-06) for every cancelled auction.
        /// </summary>
        public Task<ResponseModel<AuctionViewModel>> CancelAsync(CancelAuctionDto dto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                var auction = await _repository.GetAsync(a => a.Id == dto.Id, cancellationToken)
                              ?? throw new NotFoundException("Entity not found");
                if (auction.Status is not (AuctionStatus.SCHEDULED or AuctionStatus.ONGOING))
                    throw new ConflictException(auction.Status == AuctionStatus.DRAFT
                        ? "A DRAFT auction is deleted, not cancelled."
                        : $"The auction is {auction.Status} and cannot be cancelled.");

                auction.Status = AuctionStatus.CANCELLED;
                auction.CancelReason = dto.Reason.Trim();
                await _repository.UpdateAsync(auction, cancellationToken);

                var property = await _propertyRepository.GetAsync(p => p.Id == auction.PropertyId, cancellationToken);
                if (property is { Status: PropertyStatus.IN_AUCTION })
                {
                    property.Status = PropertyStatus.PUBLISHED;
                    await _propertyRepository.UpdateAsync(property, cancellationToken);
                }
                await _settlement.OnCancelledAsync(auction, auction.CancelReason, cancellationToken);
                await _unitOfWork.AfterCommitAsync(() => _events.StatusChangedAsync(new AuctionStatusChangedEvent(auction.Id, auction.Status, auction.EndsAt)));
                return ResponseModel<AuctionViewModel>.Ok(_mapper.Map<AuctionViewModel>(auction));
            }, cancellationToken);

        /// <summary>
        /// The auction with its result (and the purchase offers) once the sale is over.
        /// </summary>
        public override async Task<ResponseModel<AuctionViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.READ);
            var auction = await _repository.GetAsync(a => a.Id == id, cancellationToken)
                          ?? throw new NotFoundException("Entity not found");
            var view = _mapper.Map<AuctionViewModel>(auction);

            var result = await _resultRepository.Query(r => r.AuctionId == id, cancellationToken)
                             .Include(r => r.Awards.OrderBy(a => a.Rank))
                             .FirstOrDefaultAsync(cancellationToken);
            view.Result = result == null ? null : _mapper.Map<AuctionResultViewModel>(result);
            return ResponseModel<AuctionViewModel?>.Ok(view);
        }

        public override Task<ResponseModel<AuctionViewModel?>> GetAsync(Expression<Func<Auction, bool>> predicate, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public override Task<ResponseModel<List<AuctionViewModel>>> GetAllAsync(Expression<Func<Auction, bool>>? predicate = null, int pageIndex = 0, int pageSize = Models.Requests.Abstractions.BaseGetListQuery.DefaultPageSize, Func<IQueryable<Auction>, IOrderedQueryable<Auction>>? orderBy = null, CancellationToken cancellationToken = default)
            // Default: soonest start first, as in a schedule.
            => base.GetAllAsync(predicate, pageIndex, pageSize, orderBy ?? (q => q.OrderByDescending(a => a.StartsAt).ThenByDescending(a => a.Id)), cancellationToken);

        /// <summary>
        /// The auction page for visitors; a draft is not visible.
        /// </summary>
        public async Task<ResponseModel<PublicAuctionDetailViewModel>> GetPublicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var auction = await _repository.Query(a => a.Id == id && a.Status != AuctionStatus.DRAFT, cancellationToken)
                              .FirstOrDefaultAsync(cancellationToken)
                          ?? throw new NotFoundException("Auction not found");
            var propertyTitle = await _propertyRepository.Query(p => p.Id == auction.PropertyId, cancellationToken)
                                    .Select(p => p.Title).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
            var participants = await _participantRepository.Query(p => p.AuctionId == id && p.Status == ParticipantStatus.APPROVED, cancellationToken)
                                   .CountAsync(cancellationToken);
            var bids = await _bidRepository.Query(b => b.AuctionId == id, cancellationToken).CountAsync(cancellationToken);
            var result = await _resultRepository.Query(r => r.AuctionId == id, cancellationToken).FirstOrDefaultAsync(cancellationToken);

            var summary = _mapper.Map<PublicAuctionSummaryViewModel>(auction);
            return ResponseModel<PublicAuctionDetailViewModel>.Ok(new PublicAuctionDetailViewModel
            {
                Id = summary.Id, LotNumber = summary.LotNumber, Status = summary.Status,
                StartingPrice = summary.StartingPrice, CurrentPrice = summary.CurrentPrice, MinBidStep = summary.MinBidStep,
                DepositAmount = summary.DepositAmount, ApplicationDeadline = summary.ApplicationDeadline,
                StartsAt = summary.StartsAt, EndsAt = summary.EndsAt,
                Title = auction.Title,
                Description = auction.Description,
                ExtensionMinutes = auction.ExtensionMinutes,
                MinNextBid = BidService.MinNextBid(auction),
                PropertyId = auction.PropertyId,
                PropertyTitle = propertyTitle,
                ParticipantCount = participants,
                BidCount = bids,
                Outcome = result?.Outcome,
                FinalPrice = result?.FinalPrice,
            });
        }

        // ---------------- helpers

        private async Task EnsurePermissionAsync(OperationType operation)
        {
            if (!await _permissionService.Check($"{operation}_{nameof(Auction).ToUpper()}"))
                throw new ForbiddenException("You do not have permission for this operation.");
        }

        /// <summary>
        /// Times in order and in the future, a minimum duration, and a property that exists and is not sold or archived.
        /// A property can have only one live auction (the unique index), so a second one is a 409 here already.
        /// </summary>
        private async Task<Property> EnsureValidAsync(CreateAuctionDto dto, Guid? selfId, CancellationToken cancellationToken)
            => await EnsureValidAsync(dto.PropertyId, dto.StartingPrice, dto.DepositAmount, dto.ApplicationDeadline, dto.StartsAt, dto.EndsAt, selfId, cancellationToken);

        private async Task<Property> EnsureValidAsync(UpdateAuctionDto dto, Guid? selfId, CancellationToken cancellationToken)
            => await EnsureValidAsync(dto.PropertyId, dto.StartingPrice, dto.DepositAmount, dto.ApplicationDeadline, dto.StartsAt, dto.EndsAt, selfId, cancellationToken);

        private async Task<Property> EnsureValidAsync(Guid propertyId, decimal startingPrice, decimal depositAmount, DateTime applicationDeadline, DateTime startsAt, DateTime endsAt, Guid? selfId, CancellationToken cancellationToken)
        {
            if (applicationDeadline > startsAt)
                throw new BadRequestException("Applications must close before the sale starts (ApplicationDeadline <= StartsAt).");
            if (depositAmount >= startingPrice)
                throw new BadRequestException("The deposit must be less than the starting price: it counts towards the price.");
            if (endsAt - startsAt < MinDuration)
                throw new BadRequestException($"The sale must last at least {MinDuration.TotalMinutes:0} minutes.");
            if (startsAt <= DateTime.UtcNow)
                throw new BadRequestException("The sale must start in the future.");

            var property = await _propertyRepository.GetAsync(p => p.Id == propertyId, cancellationToken)
                           ?? throw new BadRequestException("The property does not exist.");
            if (property.Status is PropertyStatus.SOLD or PropertyStatus.ARCHIVED)
                throw new BadRequestException($"The property is {property.Status} and cannot be auctioned.");

            AuctionStatus[] live = [AuctionStatus.DRAFT, AuctionStatus.SCHEDULED, AuctionStatus.ONGOING];
            if (await _repository.AnyAsync(a => a.PropertyId == propertyId && live.Contains(a.Status) && a.Id != selfId, cancellationToken))
                throw new ConflictException("The property already has an auction that is not over.");
            return property;
        }

        /// <summary>
        /// "2026-000125": the year and the number of lots so far (deleted ones included, so a number is never reused).
        /// Two lots created at the same moment may pick the same number: one gets 409 and tries again.
        /// </summary>
        private async Task<string> NextLotNumberAsync(CancellationToken cancellationToken)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"{year}-";
            var count = await _repository.Query(cancellationToken: cancellationToken).IgnoreQueryFilters()
                            .CountAsync(a => a.LotNumber.StartsWith(prefix), cancellationToken);
            return $"{prefix}{count + 1:D6}";
        }
    }
}
