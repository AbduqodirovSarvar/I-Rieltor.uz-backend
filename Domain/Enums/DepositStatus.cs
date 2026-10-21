namespace Domain.Enums
{
    /// <summary>
    /// Ishtirokchi zakalatining holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum DepositStatus
    {
        /// <summary>
        /// To'lanmagan.
        /// </summary>
        NOT_PAID,
        /// <summary>
        /// To'langan.
        /// </summary>
        PAID,
        /// <summary>
        /// Qaytarilgan (yutqazgan yoki auksion bo'lmagan).
        /// </summary>
        REFUNDED,
        /// <summary>
        /// Qaytarilmaydi (g'olib shartnomadan voz kechgan).
        /// </summary>
        FORFEITED
    }
}
