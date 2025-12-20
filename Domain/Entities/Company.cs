using Domain.Abstractions;
using Domain.Behaviours;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Company : AudiTableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        [Url]
        public string Location { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string PhoneNumber { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string PhoneNumber2 { get; set; } = string.Empty;
        [PhoneNumberValidation]
        public string Email { get; set; } = string.Empty;
        public Guid? LogoFileId { get; set; }
        public UploadedFile? LogoFile { get; set; } = null;
    }
}
