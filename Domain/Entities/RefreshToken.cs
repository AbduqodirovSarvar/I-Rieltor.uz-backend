using Domain.Abstractions;
using System;

namespace Domain.Entities
{
    /// <summary>
    /// Foydalanuvchi sessiyasi: qisqa muddatli access token tugaganda yangisini olish uchun beriladigan token.
    /// Har bir foydalanishda almashtiriladi (rotatsiya): eski token bekor qilinadi, o'rniga yangisi beriladi.
    /// </summary>
    public class RefreshToken : AudiTableEntity
    {
        /// <summary>
        /// Token egasi bo'lgan foydalanuvchining Id'si (FK).
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Token egasi (navigatsiya).
        /// </summary>
        public User? User { get; set; }

        /// <summary>
        /// Tokenning SHA-256 hash'i (hex). Tokenning o'zi saqlanmaydi:
        /// baza sizib chiqsa ham undan sessiyani tiklab bo'lmaydi. Noyob.
        /// </summary>
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Token muddati tugaydigan vaqt (UTC).
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Token bekor qilingan vaqt (UTC): rotatsiya, logout yoki parol almashishi.
        /// Null bo'lsa token hali bekor qilinmagan.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Rotatsiyada shu token o'rniga berilgan yangi tokenning Id'si. Logout'da null qoladi.
        /// </summary>
        public Guid? ReplacedByTokenId { get; set; }

        /// <summary>
        /// Token berilgan so'rovning IP manzili (nazorat uchun).
        /// </summary>
        public string? CreatedByIp { get; set; }
    }
}
