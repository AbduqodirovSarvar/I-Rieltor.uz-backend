namespace Domain.Enums
{
    /// <summary>
    /// To'lov maqsadi. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum PaymentPurpose
    {
        /// <summary>
        /// Auksionda qatnashish uchun zakalat.
        /// </summary>
        DEPOSIT,
        /// <summary>
        /// G'olibning yakuniy to'lovi (narxdan zakalat ayirib).
        /// </summary>
        FINAL_PAYMENT,
        /// <summary>
        /// Zakalatni qaytarish.
        /// </summary>
        REFUND
    }
}
