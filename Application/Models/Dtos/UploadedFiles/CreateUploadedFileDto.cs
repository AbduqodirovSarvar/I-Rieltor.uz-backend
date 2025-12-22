using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
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
        public IFormFile File { get; set; } = null!;
        public string? Description { get; set; } = null;
        public string? Alt { get; set; } = null;
        public string EntityName { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
    }
}
