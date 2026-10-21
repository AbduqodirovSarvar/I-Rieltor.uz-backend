using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Companies
{
    /// <summary>
    /// Replaces the company's details (one settings form, so every field is sent); a new Logo replaces the old one,
    /// no Logo keeps it.
    /// </summary>
    public record UpdateCompanyDto
        : BaseUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;
        [Required]
        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;
        [MaxLength(2000)]
        public string? Mission { get; set; }
        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = string.Empty;
        /// <summary>Link to the office on a map (Google/Yandex Maps URL).</summary>
        [Required]
        [Url]
        [MaxLength(1000)]
        public string Location { get; set; } = string.Empty;
        [Required]
        [PhoneNumberValidation]
        public string PhoneNumber { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string? PhoneNumber2 { get; set; }
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; }
        public IFormFile? Logo { get; set; }
        [Range(-90.0, 90.0)]
        public decimal? Latitude { get; set; }
        [Range(-180.0, 180.0)]
        public decimal? Longitude { get; set; }
    }
}
