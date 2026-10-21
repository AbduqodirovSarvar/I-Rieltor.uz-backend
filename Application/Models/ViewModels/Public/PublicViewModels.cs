using Domain.Enums;

namespace Application.Models.ViewModels.Public
{
    // What anonymous visitors of the web site get. Unlike the admin view models there are no audit fields
    // (CreatedBy/UpdatedBy are admin user Ids) and no storage details of files (Path, EntityName, EntityId).

    public record PublicFileViewModel
    {
        public string Url { get; init; } = string.Empty;
        public string? Alt { get; init; }
        public FileMimeType Type { get; init; }
    }

    public record PublicSlideViewModel
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public int Order { get; init; }
        public int DurationMs { get; init; }
        public PublicFileViewModel? Image { get; init; }
    }

    public record PublicCompanyViewModel
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string? Mission { get; init; }
        public string Address { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string? PhoneNumber2 { get; init; }
        public string? Email { get; init; }
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public PublicFileViewModel? Logo { get; init; }
    }
}
