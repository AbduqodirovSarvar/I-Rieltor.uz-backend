namespace Domain.Enums
{
    /// <summary>
    /// Obyektga biriktirilgan fayl turi. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum PropertyMediaKind
    {
        /// <summary>
        /// Rasm (galereyada ko'rsatiladi).
        /// </summary>
        IMAGE,
        /// <summary>
        /// Hujjat (kadastr, baholash hisoboti va hokazo).
        /// </summary>
        DOCUMENT,
        /// <summary>
        /// Video.
        /// </summary>
        VIDEO
    }
}
