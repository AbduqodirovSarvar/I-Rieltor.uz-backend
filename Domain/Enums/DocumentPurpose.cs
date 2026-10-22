namespace Domain.Enums
{
    /// <summary>
    /// Shaxsiy hujjatning maqsadi. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum DocumentPurpose
    {
        /// <summary>
        /// Shaxsni tasdiqlovchi hujjat (pasport, ID-karta).
        /// </summary>
        IDENTITY,
        /// <summary>
        /// Ishonchnoma (yuridik shaxs nomidan ariza berganda).
        /// </summary>
        POWER_OF_ATTORNEY,
        /// <summary>
        /// To'lov cheki yoki bank o'tkazmasi tasdig'i.
        /// </summary>
        PAYMENT_RECEIPT,
        /// <summary>
        /// Auksionda qatnashish arizasiga ilova.
        /// </summary>
        APPLICATION_ATTACHMENT,
        OTHER
    }
}
