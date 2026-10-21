using System.ComponentModel.DataAnnotations;
using Application.Models.Dtos.Abstractions;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Slides
{
    public record CreateSlideDto : BaseCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; init; } = string.Empty;
        [MaxLength(300)]
        public string Subtitle { get; init; } = string.Empty;
        [MaxLength(4000)]
        public string Content { get; init; } = string.Empty;
        [Required]
        public IFormFile File { get; init; } = null!;
        /// <summary>Position in the carousel, ascending.</summary>
        [Range(0, 10_000)]
        public int Order { get; init; }
        /// <summary>How long the slide is shown, in milliseconds (0.5–60 s).</summary>
        [Range(500, 60_000)]
        public int DurationMs { get; init; } = 3000;
    }
}
