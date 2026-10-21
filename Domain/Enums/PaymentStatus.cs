namespace Domain.Enums
{
    /// <summary>
    /// To'lov holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum PaymentStatus
    {
        /// <summary>
        /// Yaratilgan, to'lov tizimidan javob kutilmoqda.
        /// </summary>
        PENDING,
        /// <summary>
        /// To'langan.
        /// </summary>
        PAID,
        /// <summary>
        /// To'lov o'tmagan.
        /// </summary>
        FAILED,
        /// <summary>
        /// Bekor qilingan (to'lov tizimi yoki admin).
        /// </summary>
        CANCELLED,
        /// <summary>
        /// Pul qaytarilgan.
        /// </summary>
        REFUNDED
    }
}
