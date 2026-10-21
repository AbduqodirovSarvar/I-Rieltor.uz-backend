using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UploadedFiles;
using Domain.Behaviours;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.Companies
{
    public record CompanyViewModel : BaseViewModel
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
        public string? PhoneNumber2 { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        public Guid? LogoFileId { get; set; }
        public UploadedFileViewModel? LogoFile { get; set; } = null;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
    }
}
