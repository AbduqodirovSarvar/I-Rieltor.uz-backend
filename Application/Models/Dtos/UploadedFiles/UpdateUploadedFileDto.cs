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
    public record UpdateUploadedFileDto
        : BaseUpdateDto
    {
        public IFormFile? File { get; set; }
        public string? Description { get; set; } = null;
        public string? Alt { get; set; } = null;
    }
}
