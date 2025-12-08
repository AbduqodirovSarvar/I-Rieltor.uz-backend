using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UploadedFile : AudiTableEntity
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
