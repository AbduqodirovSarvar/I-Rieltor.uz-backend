using System.ComponentModel.DataAnnotations;

namespace Application.Models.Requests.Properties
{
    /// <summary>
    /// Filters of the public catalog. Every filter is optional; together they must all match.
    /// </summary>
    public record PublicPropertyFilter
    {
        public const int MaxPageSize = 50;

        /// <summary>The category or one of its direct subcategories.</summary>
        public Guid? CategoryId { get; init; }

        /// <summary>Exact region name, letter case ignored.</summary>
        [MaxLength(100)]
        public string? Region { get; init; }

        [MaxLength(100)]
        public string? District { get; init; }

        [Range(0.0, 10_000_000.0)]
        public decimal? MinArea { get; init; }

        [Range(0.0, 10_000_000.0)]
        public decimal? MaxArea { get; init; }

        [Range(0, 1000)]
        public int? MinRooms { get; init; }

        [Range(0, 1000)]
        public int? MaxRooms { get; init; }

        /// <summary>Price of the current auction (the current bid, or the starting price before any bid), so'm.</summary>
        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal? MinPrice { get; init; }

        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal? MaxPrice { get; init; }

        /// <summary>Only properties with a scheduled or running auction.</summary>
        public bool OnlyInAuction { get; init; }

        /// <summary>Also show sold properties (history).</summary>
        public bool IncludeSold { get; init; }

        /// <summary>Looked for in the title, address, region, district and cadastral number.</summary>
        [MaxLength(100)]
        public string? SearchText { get; init; }

        /// <summary>newest (default), price_asc, price_desc, area_asc, area_desc, auction_soon.</summary>
        [MaxLength(20)]
        public string? Sort { get; init; }

        [Range(0, 1_000_000)]
        public int PageIndex { get; init; }

        [Range(1, MaxPageSize)]
        public int PageSize { get; init; } = 20;
    }
}
