namespace Domain.Enums
{
    /// <summary>
    /// Auksion natijasi. Bazada matn sifatida saqlanadi.
    /// </summary>
    public enum AuctionOutcome
    {
        /// <summary>
        /// Xaridor aniqlangan (g'olib yoki taklifni qabul qilgan ikkinchi o'rin), yakuniy to'lov kutilmoqda.
        /// </summary>
        AWAITING_PAYMENT,
        /// <summary>
        /// G'olib to'lamadi: obyekt ikkinchi o'rindagi ishtirokchiga taklif qilingan, uning javobi kutilmoqda. Xaridor hali yo'q.
        /// </summary>
        AWAITING_RUNNER_UP,
        /// <summary>
        /// Xaridor to'lagan, obyekt sotilgan.
        /// </summary>
        SOLD,
        /// <summary>
        /// Xaridorlar voz kechgan yoki to'lamagan (g'olib, kerak bo'lsa ikkinchi o'rin ham).
        /// </summary>
        NOT_SOLD,
        /// <summary>
        /// Tasdiqlangan ishtirokchi bo'lmagan.
        /// </summary>
        NO_PARTICIPANTS,
        /// <summary>
        /// Birorta taklif berilmagan.
        /// </summary>
        NO_BIDS
    }
}
