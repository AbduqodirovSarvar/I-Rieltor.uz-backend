using Application.Models.Dtos.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace Application.Models.Dtos.PropertyCategories
{
    public record CreatePropertyCategoryDto : BaseCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        /// <summary>Parent category; empty for a top-level one.</summary>
        public Guid? ParentCategoryId { get; set; }
        [Range(0, 10_000)]
        public int SortOrder { get; set; }
    }

    /// <summary>Full replacement, like the other admin forms.</summary>
    public record UpdatePropertyCategoryDto : BaseUpdateDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        [Range(0, 10_000)]
        public int SortOrder { get; set; }
    }

    public record DeletePropertyCategoryDto : BaseDeleteDto
    {
    }
}
