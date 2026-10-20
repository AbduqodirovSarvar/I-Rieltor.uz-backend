using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Users
{
    public record CreateUserDto : BaseCreateDto
    {
        public ClientType Type { get; set; } = ClientType.NONE;
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = null;
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_.]{2,49}$", ErrorMessage = "Login must start with a letter and contain 3-50 letters, digits, '_' or '.'.")]
        public string? Login { get; set; } = null;
        public string? LegalName { get; set; } = null;
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;
        public string? Address { get; set; } = null;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        [PasswordValidation]
        public string Password { get; set; } = string.Empty;
    }
}
