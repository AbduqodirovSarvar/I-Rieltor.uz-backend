namespace Domain.Enums
{
    /// <summary>
    /// Auksionda qatnashish arizasining holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum ParticipantStatus
    {
        /// <summary>
        /// Ariza yuborilgan, admin ko'rib chiqmoqda.
        /// </summary>
        PENDING,
        /// <summary>
        /// Tasdiqlangan: zakalat to'langach taklif bera oladi.
        /// </summary>
        APPROVED,
        /// <summary>
        /// Rad etilgan.
        /// </summary>
        REJECTED,
        /// <summary>
        /// Ishtirokchi arizasini qaytarib olgan.
        /// </summary>
        WITHDRAWN
    }
}
