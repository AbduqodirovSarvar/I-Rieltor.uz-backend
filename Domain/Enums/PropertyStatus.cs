namespace Domain.Enums
{
    /// <summary>
    /// Ko'chmas mulk obyektining holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum PropertyStatus
    {
        /// <summary>
        /// Qoralama: faqat adminlar ko'radi.
        /// </summary>
        DRAFT,
        /// <summary>
        /// Katalogda ko'rinadi, auksionga qo'yilmagan.
        /// </summary>
        PUBLISHED,
        /// <summary>
        /// Faol auksionda (rejalashtirilgan yoki o'tmoqda).
        /// </summary>
        IN_AUCTION,
        /// <summary>
        /// Auksionda sotilgan.
        /// </summary>
        SOLD,
        /// <summary>
        /// Katalogdan olingan.
        /// </summary>
        ARCHIVED
    }
}
