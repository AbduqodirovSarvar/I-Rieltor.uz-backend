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
    /// Tizim foydalanuvchisi: jismoniy shaxs, yuridik shaxs yoki administrator.
    /// Shaxsiy va aloqa ma'lumotlari, kirish ma'lumotlari va roli shu yerda saqlanadi.
    /// </summary>
    public class User : AudiTableEntity
    {
        /// <summary>
        /// Mijoz turi: jismoniy shaxs (INDIVIDUAL), yuridik shaxs (LEGAL) yoki aniqlanmagan (NONE).
        /// </summary>
        public ClientType Type { get; set; } = ClientType.NONE;

        /// <summary>
        /// To'liq ism (F.I.Sh.). Yuridik shaxsda tashkilot nomi yoziladi.
        /// </summary>
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Tizimga kirish uchun login. Noyob va ixtiyoriy: E-imzo orqali ro'yxatdan o'tganlarda bo'lmaydi.
        /// </summary>
        public string? Login { get; set; } = null;

        /// <summary>
        /// Yuridik shaxsning rasmiy nomi. Faqat LEGAL turdagi mijozlar uchun.
        /// </summary>
        public string? LegalName { get; set; } = null;

        /// <summary>
        /// Jismoniy shaxsning PINFL'i (14 raqam) yoki yuridik shaxsning INN'i (9 raqam).
        /// Noyob. E-imzo orqali kirishda foydalanuvchi shu maydon bo'yicha topiladi.
        /// </summary>
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;

        /// <summary>
        /// Asosiy telefon raqami, xalqaro formatda (masalan +998901234567). Noyob.
        /// </summary>
        [PhoneNumberValidation]
        public string? Phone { get; set; } = null;

        /// <summary>
        /// Qo'shimcha telefon raqami (ixtiyoriy).
        /// </summary>
        [PhoneNumberValidation]
        public string? SecondaryPhone { get; set; } = null;

        /// <summary>
        /// Yashash yoki faoliyat manzili (ixtiyoriy).
        /// </summary>
        public string? Address { get; set; } = null;

        /// <summary>
        /// Elektron pochta manzili. Noyob, login sifatida va xat yuborish uchun ishlatiladi.
        /// </summary>
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Parolning hash qiymati (PBKDF2). Parolning o'zi hech qachon saqlanmaydi.
        /// Ro'yxat filtrlarida ishlatib bo'lmaydi.
        /// </summary>
        [NotFilterable]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Foydalanuvchi rolining Id'si (FK). Rol foydalanuvchi qaysi amallarni bajara olishini belgilaydi.
        /// </summary>
        public Guid? UserRoleId { get; set; }

        /// <summary>
        /// Foydalanuvchi roli (navigatsiya). Rol nomi va ruxsatlarga shu orqali murojaat qilinadi.
        /// </summary>
        public UserRole? UserRole { get; set; }
    }
}
