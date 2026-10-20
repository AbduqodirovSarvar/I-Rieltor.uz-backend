using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UserRoles;
using Domain.Behaviours;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.ViewModels.Users
{
    public record UserViewModel : BaseViewModel
    {
        public ClientType Type { get; set; }
        /// <summary>
        /// "Familiya Ism Otasining ismi" as stored; the user entity keeps the name in one field.
        /// </summary>
        public string FullName { get; set; } = string.Empty;
        public string? Login { get; set; } = null;
        public string? LegalName { get; set; } = null;
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;
        public string? Address { get; set; } = null;
        [EmailAddress]
        public string? Email { get; set; }
        public Guid? UserRoleId { get; set; }
        public UserRoleViewModel? UserRole { get; set; }
    }
}
