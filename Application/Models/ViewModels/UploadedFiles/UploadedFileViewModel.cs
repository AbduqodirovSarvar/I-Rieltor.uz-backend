using Application.Models.ViewModels.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.UploadedFiles
{
    public record UploadedFileViewModel : BaseViewModel
    {
        public string Name { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public FileMimeType Type { get; init; }
        public long Size { get; init; }
        public string? Description { get; init; }
        public string? Alt { get; init; }
        public string EntityName { get; init; } = string.Empty;
        public Guid? EntityId { get; init; }
        public byte[]? Content { get; set; }
    }
}
