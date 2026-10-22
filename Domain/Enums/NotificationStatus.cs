namespace Domain.Enums
{
    /// <summary>
    /// Bildirishnomaning yuborilish holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum NotificationStatus
    {
        /// <summary>
        /// Navbatda: yuborilishi yoki qayta urinishi kutilmoqda.
        /// </summary>
        PENDING,
        /// <summary>
        /// Yuborildi.
        /// </summary>
        SENT,
        /// <summary>
        /// Bir necha urinishdan keyin ham yuborilmadi.
        /// </summary>
        FAILED,
        /// <summary>
        /// Yuborilmadi: kanal o'chirilgan.
        /// </summary>
        SKIPPED
    }
}
