namespace Domain.Enums
{
    /// <summary>
    /// Sotish taklifining (<see cref="Domain.Entities.AuctionAward"/>) holati. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum AwardStatus
    {
        /// <summary>
        /// Ikkinchi o'ringa taklif qilingan, javobi kutilmoqda.
        /// </summary>
        OFFERED,
        /// <summary>
        /// Qabul qilingan, yakuniy to'lov kutilmoqda. G'olib uchun darhol shu holat: u savdoda yutib xaridga majbur.
        /// </summary>
        ACCEPTED,
        /// <summary>
        /// Yakuniy to'lov qilingan.
        /// </summary>
        PAID,
        /// <summary>
        /// Ikkinchi o'rin taklifni rad etgan: zakalati qaytariladi.
        /// </summary>
        DECLINED,
        /// <summary>
        /// Majburiyat bajarilmagan (g'olib voz kechgan yoki qabul qilgan xaridor muddatida to'lamagan): zakalat qaytarilmaydi.
        /// </summary>
        DEFAULTED,
        /// <summary>
        /// Ikkinchi o'rin muddatida javob bermagan: rad etgan deb olinadi, zakalati qaytariladi.
        /// </summary>
        EXPIRED
    }
}
