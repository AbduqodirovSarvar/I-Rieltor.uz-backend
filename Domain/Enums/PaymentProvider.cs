namespace Domain.Enums
{
    /// <summary>
    /// To'lov qabul qilingan tizim. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum PaymentProvider
    {
        /// <summary>
        /// Click.
        /// </summary>
        CLICK,
        /// <summary>
        /// Payme.
        /// </summary>
        PAYME,
        /// <summary>
        /// Uzum Bank.
        /// </summary>
        UZUM,
        /// <summary>
        /// Bank o'tkazmasi (rekvizitlar bo'yicha).
        /// </summary>
        BANK_TRANSFER,
        /// <summary>
        /// Admin qo'lda kiritgan.
        /// </summary>
        MANUAL
    }
}
