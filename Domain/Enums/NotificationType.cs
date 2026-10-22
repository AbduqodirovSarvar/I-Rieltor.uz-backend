namespace Domain.Enums
{
    /// <summary>
    /// Bildirishnoma turi (shablon). Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Ariza tasdiqlandi.
        /// </summary>
        APPLICATION_APPROVED,
        /// <summary>
        /// Ariza rad etildi.
        /// </summary>
        APPLICATION_REJECTED,
        /// <summary>
        /// Zakalat qabul qilindi.
        /// </summary>
        DEPOSIT_RECEIVED,
        /// <summary>
        /// Savdo tez orada boshlanadi.
        /// </summary>
        AUCTION_STARTING_SOON,
        /// <summary>
        /// Taklifingiz oshirildi.
        /// </summary>
        OUTBID,
        /// <summary>
        /// Siz g'olib bo'ldingiz.
        /// </summary>
        AUCTION_WON,
        /// <summary>
        /// Yakuniy to'lov muddati yaqin.
        /// </summary>
        PAYMENT_DUE_SOON,
        /// <summary>
        /// G'olib to'lamadi: obyektni sotib olish taklifi.
        /// </summary>
        RUNNER_UP_OFFER,
        /// <summary>
        /// To'lov muddatida qilinmadi, zakalat qaytarilmaydi.
        /// </summary>
        WINNER_DEFAULTED,
        /// <summary>
        /// Auksion bekor qilindi.
        /// </summary>
        AUCTION_CANCELLED,
        /// <summary>
        /// Zakalat qaytarildi.
        /// </summary>
        DEPOSIT_REFUNDED,
        /// <summary>
        /// Yakuniy to'lov qabul qilindi, obyekt sotildi.
        /// </summary>
        PAYMENT_RECEIVED
    }
}
