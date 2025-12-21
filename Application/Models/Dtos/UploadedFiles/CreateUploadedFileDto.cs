using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.UploadedFiles
{
    public record CreateUploadedFileDto
        : BaseCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        [UriValidation]
        public string Url { get; set; } = string.Empty;
        public FileMimeType Type { get; set; } = FileMimeType.Unknown;
        public long Size { get; set; }
        public string? Description { get; set; } = null;
        public string? Alt { get; set; } = null;
    }
}
