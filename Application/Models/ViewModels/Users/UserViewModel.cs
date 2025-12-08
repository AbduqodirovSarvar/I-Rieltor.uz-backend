using Application.Models.ViewModels.Abstractions;
using Application.Models.ViewModels.UserRole;
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
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; } = null;
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
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public Guid? UserRoleId { get; set; }
        public UserRoleViewModel? UserRole { get; set; }
    }
}
