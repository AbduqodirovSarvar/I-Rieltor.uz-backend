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
        /// Elektron imzo orqali yuborilgan imzolangan ma'lumot (base64 formatda)
        /// </summary>
        [Required]
        public string SignedData { get; init; } = null!;

        /// <summary>
        /// Foydalanuvchining E-IMZO sertifikatidan olingan ochiq kalit (public key)
        /// </summary>
        [Required]
        public string PublicKey { get; init; } = null!;

        /// <summary>
        /// Yangi parol
        /// </summary>
        [Required]
        public string NewPassword { get; init; } = null!;

        /// <summary>
        /// Yangi parolni tasdiqlash
        /// </summary>
        [Required]
        public string ConfirmPassword { get; init; } = null!;
    }
}
