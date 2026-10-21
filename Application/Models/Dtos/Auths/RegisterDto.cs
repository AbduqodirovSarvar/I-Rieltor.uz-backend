using Application.Models.Dtos.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Auths
{
    /// <summary>
    /// Self-registration with login/email and password.
    /// PINFL/INN is intentionally absent: it is bound to an account only through E-Imzo,
    /// so nobody can register someone else's identifier.
    /// </summary>
    /// <summary>
    /// Sign-up with login and password. Nothing here is verified, so the account cannot claim to be a person or a
    /// company: it gets <see cref="ClientType.NONE"/>; INDIVIDUAL/LEGAL, the PINFL/INN and the company name come only
    /// from an E-IMZO certificate (sign-up via E-IMZO).
    /// </summary>
    public record RegisterDto : BaseCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? MiddleName { get; set; } = null;
        /// <summary>
        /// Starts with a letter and has no "@", so it can never equal another user's email or PINFL
        /// (sign-in looks the value up in all three columns).
        /// </summary>
        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_.]{2,49}$", ErrorMessage = "Login must start with a letter and contain 3-50 letters, digits, '_' or '.'.")]
        public string? Login { get; set; } = null;
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;
        [MaxLength(300)]
        public string? Address { get; set; } = null;
        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [PasswordValidation]
        public string Password { get; set; } = string.Empty;
    }
}
