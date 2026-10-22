using Domain.Behaviours;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.PrivateDocuments
{
    public record UploadPrivateDocumentDto
    {
        [Required]
        public IFormFile File { get; init; } = null!;
        public DocumentPurpose Purpose { get; init; } = DocumentPurpose.OTHER;
        /// <summary>The auction it belongs to (an application attachment, for example).</summary>
        public Guid? AuctionId { get; init; }
        /// <summary>The payment it belongs to (a bank transfer receipt); must be the caller's own.</summary>
        public Guid? PaymentId { get; init; }
        [MaxLength(500)]
        public string? Description { get; init; }
    }

    public record DeletePrivateDocumentDto
    {
        [Required]
        [NotEmptyGuid]
        public Guid Id { get; init; }
    }
}
