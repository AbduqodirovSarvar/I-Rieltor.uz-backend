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
    public class User : AuditableEntity
    {
        /// <summary>
        /// Mijoz turi: jismoniy shaxs (INDIVIDUAL), yuridik shaxs (LEGAL) yoki aniqlanmagan (NONE).
        /// </summary>
        public ClientType Type { get; set; } = ClientType.NONE;

        /// <summary>
        /// To'liq ism (F.I.Sh.). Yuridik shaxsda tashkilot nomi yoziladi.
        /// </summary>
        [Searchable]
        public string FullName { get; set; } = string.Empty;

        /// <summary>
        /// Tizimga kirish uchun login. Noyob va ixtiyoriy: E-imzo orqali ro'yxatdan o'tganlarda bo'lmaydi.
        /// </summary>
        [Searchable]
        public string? Login { get; set; } = null;

        /// <summary>
        /// Yuridik shaxsning rasmiy nomi. Faqat LEGAL turdagi mijozlar uchun.
        /// </summary>
        [Searchable]
        public string? LegalName { get; set; } = null;

        /// <summary>
        /// Jismoniy shaxsning PINFL'i (14 raqam) yoki yuridik shaxsning INN'i (9 raqam).
        /// Noyob. E-imzo orqali kirishda foydalanuvchi shu maydon bo'yicha topiladi.
        /// </summary>
        [Searchable]
        [INNPINFLValidation]
        public string? INNPINFL { get; set; } = null;

        /// <summary>
        /// Asosiy telefon raqami, xalqaro formatda (masalan +998901234567). Noyob.
        /// </summary>
        [Searchable]
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
        /// E-imzo orqali ro'yxatdan o'tganlarda bo'lmasligi mumkin (null).
        /// </summary>
        [Searchable]
        [EmailAddress]
        public string? Email
        {
            get => _email;
            // Bo'sh joylar olib tashlanadi, bo'sh qiymat "email yo'q" (null). Katta-kichik harf farqi
            // bazada hal qilinadi: ustun turi citext, ya'ni "A@x.uz" va "a@x.uz" bitta manzil.
            set => _email = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        private string? _email;

        /// <summary>
        /// Email tasdiqlangan vaqt (UTC). Null: email yo'q yoki hali tasdiqlanmagan.
        /// Email o'zgarsa yana null bo'ladi va yangi manzilga tasdiqlash havolasi yuboriladi.
        /// </summary>
        public DateTime? EmailConfirmedAt { get; set; } = null;

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
