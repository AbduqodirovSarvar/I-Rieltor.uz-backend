using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class PropertyFiles : AudiTableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid PropertyId { get; set; }
        public Property? Property { get; set; } = null;
        public Guid UploadedFileId { get; set; }
        public UploadedFile? UploadedFile { get; set; } = null;
    }
}
