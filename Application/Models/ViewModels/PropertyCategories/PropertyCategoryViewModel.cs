using Application.Models.ViewModels.Abstractions;

namespace Application.Models.ViewModels.PropertyCategories
{
    public record PropertyCategoryViewModel : BaseViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public int SortOrder { get; set; }
    }
}
