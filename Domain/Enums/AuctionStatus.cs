namespace Domain.Enums
{
    /// <summary>
    /// Auksion holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum AuctionStatus
    {
        /// <summary>
        /// Qoralama: e'lon qilinmagan, o'zgartirish mumkin.
        /// </summary>
        DRAFT,
        /// <summary>
        /// E'lon qilingan: arizalar qabul qilinadi, savdo hali boshlanmagan.
        /// </summary>
        SCHEDULED,
        /// <summary>
        /// Savdo ketmoqda: taklif (bid) qabul qilinadi.
        /// </summary>
        ONGOING,
        /// <summary>
        /// Savdo tugagan, natija (AuctionResult) bor.
        /// </summary>
        FINISHED,
        /// <summary>
        /// Bo'lib o'tmagan (masalan ishtirokchi yoki taklif bo'lmagan).
        /// </summary>
        FAILED,
        /// <summary>
        /// Bekor qilingan.
        /// </summary>
        CANCELLED
    }
}
