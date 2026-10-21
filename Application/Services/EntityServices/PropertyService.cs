using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Interfaces.EntityServices;
using Application.Interfaces.Repositories;
using Application.Models.Dtos.Properties;
using Application.Models.Dtos.UploadedFiles;
using Application.Models.Requests.Abstractions;
using Application.Models.Requests.Properties;
using Application.Models.Results;
using Application.Models.ViewModels.Properties;
using Application.Models.ViewModels.Public;
using Application.Services.EntityServices.Abstractions;
using Domain.Entities;
using Domain.Enums;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.Services.EntityServices
{
    public class PropertyService(
        IPropertyRepository repository,
        IPermissionService permissionService,
        IMapper mapper,
        IPropertyCategoryRepository categoryRepository,
        IPropertyMediaRepository mediaRepository,
        IAuctionRepository auctionRepository,
        IUploadedFileService uploadedFileService,
        IUnitOfWork unitOfWork)
        : GenericEntityService<Property,
                               CreatePropertyDto,
                               UpdatePropertyDto,
                               PropertyViewModel>(repository, permissionService, mapper), IPropertyService
    {
        private const string FileCategory = "property";

        // Visitors see published properties and those in an auction; drafts and archived ones only admins.
        private static readonly PropertyStatus[] CatalogStatuses = [PropertyStatus.PUBLISHED, PropertyStatus.IN_AUCTION];
        // Set by the auctions (P2-03), never by an admin form.
        private static readonly PropertyStatus[] SystemStatuses = [PropertyStatus.IN_AUCTION, PropertyStatus.SOLD];
        // The auction shown with a property: announced or running (a draft is not public yet).
        private static readonly AuctionStatus[] ShownAuctionStatuses = [AuctionStatus.SCHEDULED, AuctionStatus.ONGOING];

        private readonly IPropertyRepository _propertyRepository = repository;
        private readonly IPropertyCategoryRepository _categoryRepository = categoryRepository;
        private readonly IPropertyMediaRepository _mediaRepository = mediaRepository;
        private readonly IAuctionRepository _auctionRepository = auctionRepository;
        private readonly IUploadedFileService _uploadedFileService = uploadedFileService;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        // ---------------- admin: the property

        public override async Task<ResponseModel<PropertyViewModel>> CreateAsync(CreatePropertyDto dto, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.CREATE);
            if (SystemStatuses.Contains(dto.Status))
                throw new BadRequestException("A new property can be DRAFT, PUBLISHED or ARCHIVED; auctions set the other statuses.");
            await EnsureValidAsync(dto.CategoryId, dto.CadastralNumber, null, cancellationToken);

            var entity = _mapper.Map<Property>(dto);
            var result = await _repository.CreateAsync(entity, cancellationToken);
            return ResponseModel<PropertyViewModel>.Ok(_mapper.Map<PropertyViewModel>(result));
        }

        public override async Task<ResponseModel<PropertyViewModel>> UpdateAsync(UpdatePropertyDto dto, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.UPDATE);
            var entity = await _repository.GetAsync(x => x.Id == dto.Id, cancellationToken)
                         ?? throw new NotFoundException("Entity not found");

            if (dto.Status != entity.Status)
            {
                if (SystemStatuses.Contains(entity.Status))
                    throw new ConflictException($"The property is {entity.Status}; its status follows the auction.");
                if (SystemStatuses.Contains(dto.Status))
                    throw new BadRequestException("IN_AUCTION and SOLD are set by the auctions, not by hand.");
            }
            await EnsureValidAsync(dto.CategoryId, dto.CadastralNumber, entity.Id, cancellationToken);

            _mapper.Map(dto, entity);
            var result = await _repository.UpdateAsync(entity, cancellationToken);
            return ResponseModel<PropertyViewModel>.Ok(_mapper.Map<PropertyViewModel>(result));
        }

        /// <summary>
        /// Refused once the property was put up for auction (the auction history must keep its object);
        /// archive it instead. Its media go with it (rows and files).
        /// </summary>
        public override Task<ResponseModel<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.DELETE);
                var entity = await _repository.GetAsync(x => x.Id == id, cancellationToken)
                             ?? throw new NotFoundException("Entity not found");
                if (await _auctionRepository.AnyAsync(a => a.PropertyId == id, cancellationToken))
                    throw new ConflictException("The property has auctions and cannot be deleted; set its status to ARCHIVED instead.");

                foreach (var media in await _mediaRepository.GetAllAsync(m => m.PropertyId == id, cancellationToken))
                {
                    await _mediaRepository.DeleteAsync(media, cancellationToken);
                    await _uploadedFileService.DeleteAsync(media.UploadedFileId, cancellationToken);
                }
                return ResponseModel<bool>.Ok(await _repository.DeleteAsync(entity, cancellationToken));
            }, cancellationToken);

        public override Task<ResponseModel<PropertyViewModel?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => GetAsync(x => x.Id == id, cancellationToken);

        /// <summary>
        /// With its media (READ_PROPERTY is enough, READ_UPLOADEDFILE is not required).
        /// </summary>
        public override async Task<ResponseModel<PropertyViewModel?>> GetAsync(Expression<Func<Property, bool>> predicate, CancellationToken cancellationToken = default)
        {
            await EnsurePermissionAsync(OperationType.READ);
            var entity = await _repository.Query(predicate, cancellationToken)
                             .Include(p => p.Media.OrderBy(m => m.SortOrder).ThenBy(m => m.CreatedAt))
                                 .ThenInclude(m => m.UploadedFile)
                             .FirstOrDefaultAsync(cancellationToken)
                         ?? throw new NotFoundException("Entity not found");
            return ResponseModel<PropertyViewModel?>.Ok(_mapper.Map<PropertyViewModel>(entity));
        }

        // ---------------- admin: media

        public Task<ResponseModel<PropertyMediaViewModel>> AddMediaAsync(AddPropertyMediaDto dto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                if (!await _repository.AnyAsync(p => p.Id == dto.PropertyId, cancellationToken))
                    throw new NotFoundException("Property not found");
                if (dto.IsCover && dto.Kind != PropertyMediaKind.IMAGE)
                    throw new BadRequestException("Only an image can be the cover.");

                // A wrong type throws after the file is written: the rollback removes it again.
                var file = (await _uploadedFileService.CreateAsync(new CreateUploadedFileDto
                {
                    File = dto.File,
                    Alt = dto.Title,
                    EntityName = FileCategory,
                    EntityId = dto.PropertyId,
                }, cancellationToken)).Data!;
                if (!Fits(dto.Kind, file.Type))
                    throw new BadRequestException($"A {file.Type} file cannot be added as {dto.Kind}.");

                if (dto.IsCover)
                    await ClearCoverAsync(dto.PropertyId, null, cancellationToken);
                var media = await _mediaRepository.CreateAsync(new PropertyMedia
                {
                    PropertyId = dto.PropertyId,
                    UploadedFileId = file.Id,
                    Kind = dto.Kind,
                    Title = dto.Title,
                    SortOrder = dto.SortOrder,
                    IsCover = dto.IsCover,
                }, cancellationToken);
                return ResponseModel<PropertyMediaViewModel>.Ok(await MediaViewAsync(media.Id, cancellationToken));
            }, cancellationToken);

        public Task<ResponseModel<PropertyMediaViewModel>> UpdateMediaAsync(UpdatePropertyMediaDto dto, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                var media = await _mediaRepository.GetAsync(m => m.Id == dto.Id, cancellationToken)
                            ?? throw new NotFoundException("Media not found");
                if (dto.IsCover && media.Kind != PropertyMediaKind.IMAGE)
                    throw new BadRequestException("Only an image can be the cover.");

                if (dto.IsCover && !media.IsCover)
                    await ClearCoverAsync(media.PropertyId, media.Id, cancellationToken);
                media.Title = dto.Title;
                media.SortOrder = dto.SortOrder;
                media.IsCover = dto.IsCover;
                await _mediaRepository.UpdateAsync(media, cancellationToken);
                return ResponseModel<PropertyMediaViewModel>.Ok(await MediaViewAsync(media.Id, cancellationToken));
            }, cancellationToken);

        public Task<ResponseModel<bool>> DeleteMediaAsync(Guid id, CancellationToken cancellationToken = default)
            => _unitOfWork.ExecuteAsync(async () =>
            {
                await EnsurePermissionAsync(OperationType.UPDATE);
                var media = await _mediaRepository.GetAsync(m => m.Id == id, cancellationToken)
                            ?? throw new NotFoundException("Media not found");
                await _mediaRepository.DeleteAsync(media, cancellationToken);
                await _uploadedFileService.DeleteAsync(media.UploadedFileId, cancellationToken);
                return ResponseModel<bool>.Ok(true);
            }, cancellationToken);

        // ---------------- public catalog

        public async Task<ResponseModel<List<PublicPropertyListItemViewModel>>> GetPublicListAsync(PublicPropertyFilter filter, CancellationToken cancellationToken = default)
        {
            PropertyStatus[] statuses = filter.IncludeSold ? [.. CatalogStatuses, PropertyStatus.SOLD] : CatalogStatuses;
            var query = _repository.Query(p => statuses.Contains(p.Status), cancellationToken);
            var auctions = _auctionRepository.Query(a => ShownAuctionStatuses.Contains(a.Status), cancellationToken);

            if (filter.CategoryId is { } categoryId)
            {
                var categoryIds = await _categoryRepository.Query(c => c.Id == categoryId || c.ParentCategoryId == categoryId, cancellationToken)
                                       .Select(c => c.Id).ToListAsync(cancellationToken);
                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }
            if (!string.IsNullOrWhiteSpace(filter.Region))
            {
                var region = filter.Region.Trim().ToLower();
                query = query.Where(p => p.Region.ToLower() == region);
            }
            if (!string.IsNullOrWhiteSpace(filter.District))
            {
                var district = filter.District.Trim().ToLower();
                query = query.Where(p => p.District != null && p.District.ToLower() == district);
            }
            if (filter.MinArea is { } minArea) query = query.Where(p => p.AreaSqm >= minArea);
            if (filter.MaxArea is { } maxArea) query = query.Where(p => p.AreaSqm <= maxArea);
            if (filter.MinRooms is { } minRooms) query = query.Where(p => p.Rooms >= minRooms);
            if (filter.MaxRooms is { } maxRooms) query = query.Where(p => p.Rooms <= maxRooms);
            if (filter.OnlyInAuction || filter.MinPrice != null || filter.MaxPrice != null)
            {
                var minPrice = filter.MinPrice ?? 0;
                var maxPrice = filter.MaxPrice ?? decimal.MaxValue;
                query = query.Where(p => auctions.Any(a => a.PropertyId == p.Id
                    && (a.CurrentPrice ?? a.StartingPrice) >= minPrice && (a.CurrentPrice ?? a.StartingPrice) <= maxPrice));
            }
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var text = filter.SearchText.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(text)
                                      || p.Address.ToLower().Contains(text)
                                      || p.Region.ToLower().Contains(text)
                                      || (p.District != null && p.District.ToLower().Contains(text))
                                      || (p.CadastralNumber != null && p.CadastralNumber.ToLower().Contains(text)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            Expression<Func<Property, decimal?>> price = p => auctions.Where(a => a.PropertyId == p.Id).Select(a => (decimal?)(a.CurrentPrice ?? a.StartingPrice)).FirstOrDefault();
            Expression<Func<Property, DateTime?>> startsAt = p => auctions.Where(a => a.PropertyId == p.Id).Select(a => (DateTime?)a.StartsAt).FirstOrDefault();
            IOrderedQueryable<Property> ordered = (filter.Sort ?? "newest").ToLowerInvariant() switch
            {
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                // Properties without an auction (no price, no date) go last in every direction.
                "price_asc" => query.OrderBy(p => auctions.Any(a => a.PropertyId == p.Id) ? 0 : 1).ThenBy(price),
                "price_desc" => query.OrderBy(p => auctions.Any(a => a.PropertyId == p.Id) ? 0 : 1).ThenByDescending(price),
                "area_asc" => query.OrderBy(p => p.AreaSqm == null ? 1 : 0).ThenBy(p => p.AreaSqm),
                "area_desc" => query.OrderBy(p => p.AreaSqm == null ? 1 : 0).ThenByDescending(p => p.AreaSqm),
                "auction_soon" => query.OrderBy(p => auctions.Any(a => a.PropertyId == p.Id) ? 0 : 1).ThenBy(startsAt),
                _ => throw new BadRequestException("Sort must be newest, price_asc, price_desc, area_asc, area_desc or auction_soon.")
            };

            var page = await ordered.ThenByDescending(p => p.Id)
                                    .Skip(filter.PageIndex * filter.PageSize)
                                    .Take(filter.PageSize)
                                    .Include(p => p.Category)
                                    .Include(p => p.Media.Where(m => m.IsCover))
                                        .ThenInclude(m => m.UploadedFile)
                                    .ToListAsync(cancellationToken);
            var shown = await ShownAuctionsAsync(page.Select(p => p.Id).ToList(), cancellationToken);

            var items = page.Select(p => ListItem(p, shown.GetValueOrDefault(p.Id))).ToList();
            return ResponseModel<List<PublicPropertyListItemViewModel>>.Ok(items, filter.PageIndex, filter.PageSize, totalCount);
        }

        public async Task<ResponseModel<PublicPropertyDetailViewModel>> GetPublicAsync(Guid id, CancellationToken cancellationToken = default)
        {
            PropertyStatus[] visible = [.. CatalogStatuses, PropertyStatus.SOLD];
            var property = await _repository.Query(p => p.Id == id && visible.Contains(p.Status), cancellationToken)
                               .Include(p => p.Category)
                               .Include(p => p.Media.OrderBy(m => m.SortOrder).ThenBy(m => m.CreatedAt))
                                   .ThenInclude(m => m.UploadedFile)
                               .FirstOrDefaultAsync(cancellationToken)
                           ?? throw new NotFoundException("Property not found");
            await _propertyRepository.IncrementViewCountAsync(id, cancellationToken);

            var auction = (await ShownAuctionsAsync([id], cancellationToken)).GetValueOrDefault(id);
            var item = ListItem(property, auction);
            return ResponseModel<PublicPropertyDetailViewModel>.Ok(new PublicPropertyDetailViewModel
            {
                Id = item.Id, Title = item.Title, CategoryId = item.CategoryId, CategoryName = item.CategoryName, Status = item.Status,
                Region = item.Region, District = item.District, AreaSqm = item.AreaSqm, LandAreaSotix = item.LandAreaSotix,
                Rooms = item.Rooms, Floor = item.Floor, TotalFloors = item.TotalFloors, Cover = item.Cover, Auction = item.Auction,
                CreatedAt = item.CreatedAt,
                Description = property.Description,
                Address = property.Address,
                Latitude = property.Latitude,
                Longitude = property.Longitude,
                CadastralNumber = property.CadastralNumber,
                BuildYear = property.BuildYear,
                EstimatedValue = property.EstimatedValue,
                SellerName = property.SellerName,
                ViewCount = property.ViewCount + 1,
                Media = property.Media
                    .Where(m => m.UploadedFile != null)
                    .Select(m => new PublicPropertyMediaViewModel
                    {
                        Kind = m.Kind,
                        Title = m.Title,
                        IsCover = m.IsCover,
                        File = _mapper.Map<PublicFileViewModel>(m.UploadedFile!),
                    }).ToList(),
            });
        }

        // ---------------- helpers

        private PublicPropertyListItemViewModel ListItem(Property p, Auction? auction)
        {
            var cover = p.Media.FirstOrDefault(m => m.IsCover)?.UploadedFile;
            return new PublicPropertyListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Status = p.Status,
                Region = p.Region,
                District = p.District,
                AreaSqm = p.AreaSqm,
                LandAreaSotix = p.LandAreaSotix,
                Rooms = p.Rooms,
                Floor = p.Floor,
                TotalFloors = p.TotalFloors,
                Cover = cover == null ? null : _mapper.Map<PublicFileViewModel>(cover),
                Auction = auction == null ? null : _mapper.Map<PublicAuctionSummaryViewModel>(auction),
                CreatedAt = p.CreatedAt,
            };
        }

        /// <summary>The announced or running auction of each property (at most one: IX_Auctions_PropertyId_Live).</summary>
        private async Task<Dictionary<Guid, Auction>> ShownAuctionsAsync(List<Guid> propertyIds, CancellationToken cancellationToken)
            => (await _auctionRepository.Query(a => propertyIds.Contains(a.PropertyId) && ShownAuctionStatuses.Contains(a.Status), cancellationToken)
                    .ToListAsync(cancellationToken))
               .GroupBy(a => a.PropertyId)
               .ToDictionary(g => g.Key, g => g.First());

        private async Task<PropertyMediaViewModel> MediaViewAsync(Guid mediaId, CancellationToken cancellationToken)
            => _mapper.Map<PropertyMediaViewModel>(await _mediaRepository.Query(m => m.Id == mediaId, cancellationToken)
                                                       .Include(m => m.UploadedFile)
                                                       .FirstAsync(cancellationToken));

        /// <summary>The unique cover index allows one cover per property: the old one is cleared first.</summary>
        private async Task ClearCoverAsync(Guid propertyId, Guid? exceptMediaId, CancellationToken cancellationToken)
        {
            foreach (var cover in await _mediaRepository.GetAllAsync(m => m.PropertyId == propertyId && m.IsCover && m.Id != exceptMediaId, cancellationToken))
            {
                cover.IsCover = false;
                await _mediaRepository.UpdateAsync(cover, cancellationToken);
            }
        }

        private static bool Fits(PropertyMediaKind kind, FileMimeType type) => kind switch
        {
            PropertyMediaKind.IMAGE => type is FileMimeType.ImagePng or FileMimeType.ImageJpeg or FileMimeType.ImageGif,
            PropertyMediaKind.VIDEO => type is FileMimeType.VideoMp4,
            // Scans of paper documents are images.
            PropertyMediaKind.DOCUMENT => type is FileMimeType.ApplicationPdf or FileMimeType.ApplicationDocx or FileMimeType.ApplicationXlsx
                                               or FileMimeType.ImagePng or FileMimeType.ImageJpeg,
            _ => false
        };

        private async Task EnsurePermissionAsync(OperationType operation)
        {
            if (!await _permissionService.Check($"{operation}_{nameof(Property).ToUpper()}"))
                throw new ForbiddenException("You do not have permission for this operation.");
        }

        private async Task EnsureValidAsync(Guid categoryId, string? cadastralNumber, Guid? selfId, CancellationToken cancellationToken)
        {
            if (!await _categoryRepository.AnyAsync(c => c.Id == categoryId, cancellationToken))
                throw new BadRequestException("The category does not exist.");
            if (!string.IsNullOrWhiteSpace(cadastralNumber)
                && await _repository.AnyAsync(p => p.CadastralNumber == cadastralNumber && (selfId == null || p.Id != selfId), cancellationToken))
                throw new ConflictException("A property with this cadastral number already exists.");
        }
    }
}
