using Domain.Enums;

namespace Application.Models.ViewModels.Public
{
    // Catalog for anonymous visitors: no audit fields, no storage details, and only published properties.

    public record PublicPropertyCategoryViewModel
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public Guid? ParentCategoryId { get; init; }
        public int SortOrder { get; init; }
    }

    /// <summary>The property's current (draft excluded) auction, for the catalog card and the detail page.</summary>
    public record PublicAuctionSummaryViewModel
    {
        public Guid Id { get; init; }
        public string LotNumber { get; init; } = string.Empty;
        public AuctionStatus Status { get; init; }
        public decimal StartingPrice { get; init; }
        public decimal? CurrentPrice { get; init; }
        public decimal MinBidStep { get; init; }
        public decimal DepositAmount { get; init; }
        public DateTime ApplicationDeadline { get; init; }
        public DateTime StartsAt { get; init; }
        public DateTime EndsAt { get; init; }
    }

    public record PublicPropertyListItemViewModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public Guid CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public PropertyStatus Status { get; init; }
        public string Region { get; init; } = string.Empty;
        public string? District { get; init; }
        public decimal? AreaSqm { get; init; }
        public decimal? LandAreaSotix { get; init; }
        public int? Rooms { get; init; }
        public int? Floor { get; init; }
        public int? TotalFloors { get; init; }
        public PublicFileViewModel? Cover { get; init; }
        public PublicAuctionSummaryViewModel? Auction { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    public record PublicPropertyMediaViewModel
    {
        public PropertyMediaKind Kind { get; init; }
        public string? Title { get; init; }
        public bool IsCover { get; init; }
        public PublicFileViewModel File { get; init; } = new();
    }

    public record PublicPropertyDetailViewModel : PublicPropertyListItemViewModel
    {
        public string? Description { get; init; }
        public string Address { get; init; } = string.Empty;
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public string? CadastralNumber { get; init; }
        public int? BuildYear { get; init; }
        public decimal? EstimatedValue { get; init; }
        public string? SellerName { get; init; }
        public long ViewCount { get; init; }
        public List<PublicPropertyMediaViewModel> Media { get; init; } = [];
    }
}
