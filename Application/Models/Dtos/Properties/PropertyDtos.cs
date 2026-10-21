using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.Properties
{
    public record CreatePropertyDto : BaseCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(10_000)]
        public string? Description { get; set; }
        [Required]
        [NotEmptyGuid]
        public Guid CategoryId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Region { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? District { get; set; }
        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;
        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }
        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }
        [MaxLength(50)]
        public string? CadastralNumber { get; set; }
        [Range(0.01, 10_000_000.0)]
        public decimal? AreaSqm { get; set; }
        [Range(0.01, 10_000_000.0)]
        public decimal? LandAreaSotix { get; set; }
        [Range(0, 1000)]
        public int? Rooms { get; set; }
        [Range(-5, 300)]
        public int? Floor { get; set; }
        [Range(1, 300)]
        public int? TotalFloors { get; set; }
        [Range(1800, 2100)]
        public int? BuildYear { get; set; }
        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal? EstimatedValue { get; set; }
        [MaxLength(200)]
        public string? SellerName { get; set; }
        /// <summary>DRAFT, PUBLISHED or ARCHIVED; IN_AUCTION and SOLD are set by the auctions only.</summary>
        public PropertyStatus Status { get; set; } = PropertyStatus.DRAFT;
    }

    /// <summary>Full replacement of the property's own fields; media are changed with the media endpoints.</summary>
    public record UpdatePropertyDto : BaseUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(10_000)]
        public string? Description { get; set; }
        [Required]
        [NotEmptyGuid]
        public Guid CategoryId { get; set; }
        [Required]
        [MaxLength(100)]
        public string Region { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? District { get; set; }
        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;
        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }
        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }
        [MaxLength(50)]
        public string? CadastralNumber { get; set; }
        [Range(0.01, 10_000_000.0)]
        public decimal? AreaSqm { get; set; }
        [Range(0.01, 10_000_000.0)]
        public decimal? LandAreaSotix { get; set; }
        [Range(0, 1000)]
        public int? Rooms { get; set; }
        [Range(-5, 300)]
        public int? Floor { get; set; }
        [Range(1, 300)]
        public int? TotalFloors { get; set; }
        [Range(1800, 2100)]
        public int? BuildYear { get; set; }
        [Range(0.0, 1_000_000_000_000_000.0)]
        public decimal? EstimatedValue { get; set; }
        [MaxLength(200)]
        public string? SellerName { get; set; }
        /// <summary>DRAFT, PUBLISHED or ARCHIVED; IN_AUCTION and SOLD are set by the auctions only.</summary>
        public PropertyStatus Status { get; set; } = PropertyStatus.DRAFT;
    }

    public record DeletePropertyDto : BaseDeleteDto
    {
    }

    public record AddPropertyMediaDto
    {
        [Required]
        [NotEmptyGuid]
        public Guid PropertyId { get; set; }
        [Required]
        public IFormFile File { get; set; } = null!;
        /// <summary>IMAGE (png, jpg, gif), VIDEO (mp4) or DOCUMENT (pdf, docx, xlsx or a scanned image).</summary>
        public PropertyMediaKind Kind { get; set; } = PropertyMediaKind.IMAGE;
        [MaxLength(200)]
        public string? Title { get; set; }
        [Range(0, 10_000)]
        public int SortOrder { get; set; }
        /// <summary>Make it the catalog image (images only); the previous cover stops being one.</summary>
        public bool IsCover { get; set; }
    }

    public record UpdatePropertyMediaDto
    {
        [Required]
        [NotEmptyGuid]
        public Guid Id { get; set; }
        [MaxLength(200)]
        public string? Title { get; set; }
        [Range(0, 10_000)]
        public int SortOrder { get; set; }
        public bool IsCover { get; set; }
    }

    public record DeletePropertyMediaDto
    {
        [Required]
        [NotEmptyGuid]
        public Guid Id { get; set; }
    }
}
