using Application.Models.ViewModels.Abstractions;
using Domain.Enums;

namespace Application.Models.ViewModels.PrivateDocuments
{
    /// <summary>Metadata only; the stored (disk) name is never returned.</summary>
    public record PrivateDocumentViewModel : BaseViewModel
    {
        public Guid OwnerId { get; set; }
        public DocumentPurpose Purpose { get; set; }
        public Guid? AuctionId { get; set; }
        public Guid? PaymentId { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? Description { get; set; }
    }
}
