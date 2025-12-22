using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UploadedFiles;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.Slides
{
    public record SlideViewModel : BaseViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Guid UploadedFileId { get; set; }
        public UploadedFileViewModel? UploadedFile { get; set; } = null;
        public int Order { get; set; }
        public int DurationMs { get; set; } = 3000;
    }
}
