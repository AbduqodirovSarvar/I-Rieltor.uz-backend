using Domain.Behaviours;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models.Dtos.Auths
{
    public record ResetPasswordByEImzoDto
    {
        /// <summary>
        /// E-IMZO bilan imzolangan challenge (PKCS#7, Base64). Challenge `GET api/auth/eimzo/challenge` orqali olinadi.
        /// Foydalanuvchi va uning sertifikati shu imzodan aniqlanadi, alohida public key kerak emas.
        /// </summary>
        [Required]
        public string SignedData { get; init; } = null!;

        /// <summary>
        /// Yangi parol
        /// </summary>
        [Required]
        [PasswordValidation]
        public string NewPassword { get; init; } = null!;

        /// <summary>
        /// Yangi parolni tasdiqlash
        /// </summary>
        [Required]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = null!;
    }
}
