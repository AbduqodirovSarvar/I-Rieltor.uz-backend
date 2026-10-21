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
    /// <summary>
    /// Partial update: a field that is not sent (null) keeps its current value, so an admin form that shows only some
    /// fields, or a user registered through E-IMZO without email, can be edited safely. An optional value is removed
    /// only when it is named in <see cref="ClearFields"/>; an empty form field never wipes data by accident.
    /// </summary>
    public record UpdateUserDto : BaseUpdateDto
    {
        /// <summary>Optional values that may be removed with <see cref="ClearFields"/>.</summary>
        public static readonly IReadOnlySet<string> ClearableFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(Login), nameof(LegalName), nameof(INNPINFL), nameof(Phone), nameof(SecondaryPhone), nameof(Address), nameof(Email)
        };

        public ClientType? Type { get; set; } = null;

        /// <summary>
        /// The whole name as stored, e.g. a company name for a LEGAL user. Takes precedence over the name parts.
        /// </summary>
        [MaxLength(200)]
        public string? FullName { get; set; } = null;

        /// <summary>Name parts; to rebuild the name from them, send at least LastName and FirstName.</summary>
        [MaxLength(100)]
        public string? FirstName { get; set; } = null;
        [MaxLength(100)]
        public string? LastName { get; set; } = null;
        [MaxLength(100)]
        public string? MiddleName { get; set; } = null;

        [RegularExpression(@"^[A-Za-z][A-Za-z0-9_.]{2,49}$", ErrorMessage = "Login must start with a letter and contain 3-50 letters, digits, '_' or '.'.")]
        public string? Login { get; set; } = null;
        [MaxLength(200)]
        public string? LegalName { get; set; } = null;
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;
        [MaxLength(300)]
        public string? Address { get; set; } = null;
        [EmailAddress]
        [MaxLength(150)]
        public string? Email { get; set; } = null;

        /// <summary>New password; empty keeps the current one. Ends all sessions of the user.</summary>
        [PasswordValidation]
        public string? Password { get; set; } = null;

        /// <summary>Optional fields to remove, e.g. ["Phone", "Address"] (one of <see cref="ClearableFields"/>).</summary>
        public List<string> ClearFields { get; set; } = [];
    }
}
