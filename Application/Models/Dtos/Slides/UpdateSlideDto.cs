using Application.Models.Dtos.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Slides
{
    public record UpdateSlideDto : BaseUpdateDto
    {
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public IFormFile? File { get; init; }
        public int Order { get; init; }
        public int DurationMs { get; init; }
    }
}
