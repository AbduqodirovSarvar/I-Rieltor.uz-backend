using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UploadedFiles;
using Domain.Enums;

namespace Application.Models.ViewModels.Properties
{
    public record PropertyViewModel : BaseViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public PropertyStatus Status { get; set; }
        public string Region { get; set; } = string.Empty;
        public string? District { get; set; }
        public string Address { get; set; } = string.Empty;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? CadastralNumber { get; set; }
        public decimal? AreaSqm { get; set; }
        public decimal? LandAreaSotix { get; set; }
        public int? Rooms { get; set; }
        public int? Floor { get; set; }
        public int? TotalFloors { get; set; }
        public int? BuildYear { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string? SellerName { get; set; }
        public long ViewCount { get; set; }
        public List<PropertyMediaViewModel> Media { get; set; } = [];
    }

    public record PropertyMediaViewModel : BaseViewModel
    {
        public Guid PropertyId { get; set; }
        public PropertyMediaKind Kind { get; set; }
        public string? Title { get; set; }
        public int SortOrder { get; set; }
        public bool IsCover { get; set; }
        public Guid UploadedFileId { get; set; }
        public UploadedFileViewModel? UploadedFile { get; set; }
    }
}
