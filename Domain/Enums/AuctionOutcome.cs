namespace Domain.Enums
{
    /// <summary>
    /// Auksion natijasi. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum AuctionOutcome
    {
        /// <summary>
        /// G'olib aniqlangan.
        /// </summary>
        SOLD,
        /// <summary>
        /// Tasdiqlangan ishtirokchi bo'lmagan.
        /// </summary>
        NO_PARTICIPANTS,
        /// <summary>
        /// Birorta taklif berilmagan.
        /// </summary>
        NO_BIDS,
        /// <summary>
        /// G'olib to'lovdan yoki shartnomadan voz kechgan.
        /// </summary>
        WINNER_REFUSED
    }
}
