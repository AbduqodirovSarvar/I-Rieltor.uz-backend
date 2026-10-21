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
        /// To'langan. G'olib va ikkinchi o'rinniki natija aniq bo'lguncha ushlab turiladi.
        /// </summary>
        PAID,
        /// <summary>
        /// Qaytarilgan (yutqazgan, auksion bo'lmagan yoki ikkinchi o'rin taklifni rad etgan).
        /// </summary>
        REFUNDED,
        /// <summary>
        /// Qaytarilmaydi: g'olib to'lovdan voz kechgan yoki taklifni qabul qilgan xaridor to'lamagan.
        /// </summary>
        FORFEITED
    }
}
