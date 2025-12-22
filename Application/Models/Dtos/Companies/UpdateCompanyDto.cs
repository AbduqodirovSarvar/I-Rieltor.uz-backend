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
    public record UpdateCompanyDto
        : BaseUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Mission { get; set; }
        public string Address { get; set; } = string.Empty;
        [Url]
        public string Location { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string PhoneNumber { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string PhoneNumber2 { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string Email { get; set; } = string.Empty;
        public IFormFile? Logo { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
