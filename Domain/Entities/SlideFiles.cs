using Domain.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class SlideFiles : AudiTableEntity
    {
        public Guid SlideId { get; set; }
        public Slide? Slide { get; set; } = null;
        public Guid UploadedFileId { get; set; }
        public UploadedFile? UploadedFile { get; set; } = null;
    }
}
