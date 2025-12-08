using Domain.Abstractions;
using Domain.Behaviours;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    /// <summary>
    /// Represents a user in the system. Can be individual or legal entity.
    /// Contains personal, contact, authentication, and role information.
    /// </summary>
    public class User : AudiTableEntity
    {
        /// <summary>
        /// Type of client: Individual or Legal.
        /// Determines validation rules for INNPINFL/INN.
        /// </summary>
        public ClientType Type { get; set; } = ClientType.NONE;

        /// <summary>
        /// First name of the individual user.
        /// Required for Individual clients.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Last name of the individual user.
        /// Required for Individual clients.
        /// </summary>
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Optional middle name of the individual user.
        /// Only applicable for Individual clients.
        /// </summary>
        public string? MiddleName { get; set; } = null;

        /// <summary>
        /// Legal entity name. Required for Legal clients.
        /// </summary>
        public string? LegalName { get; set; } = null;

        /// <summary>
        /// INNPINFL (for Individual) or INN (for Legal) identifier.
        /// Conditional validation based on ClientType.
        /// Individual: 14 digits, Legal: 9 digits.
        /// </summary>
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;

        /// <summary>
        /// Primary phone number.
        /// Validated using PhoneNumberValidation attribute.
        /// </summary>
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;

        /// <summary>
        /// Secondary phone number (optional).
        /// Validated using PhoneNumberValidation attribute.
        /// </summary>
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;

        /// <summary>
        /// Residential or business address of the user.
        /// Optional field.
        /// </summary>
        public string? Address { get; set; } = null;

        /// <summary>
        /// Email address of the user.
        /// Must be valid email format.
        /// </summary>
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Hashed password for authentication.
        /// Should never store plain text passwords.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Foreign key to the UserRole entity.
        /// Determines the permissions and access level of the user.
        /// </summary>
        public Guid UserRoleId { get; set; }

        /// <summary>
        /// Navigation property to the user's role.
        /// Allows access to role name, description, and permissions.
        /// </summary>
        public UserRole? UserRole { get; set; }
    }
}
