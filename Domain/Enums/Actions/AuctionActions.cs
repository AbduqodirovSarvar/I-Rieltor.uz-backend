namespace Domain.Enums.Actions
{
    /// <summary>
    /// Auksion uchun ruxsat nomlari (CRUD). E'lon qilish va bekor qilish UPDATE_AUCTION ruxsatini talab qiladi.
    /// Har bir qiymat bitta policy va UserRolePermission.Permission qiymati sifatida ishlatiladi.
    /// </summary>
    public enum AuctionActions
    {
        CREATE_AUCTION,
        READ_AUCTION,
        UPDATE_AUCTION,
        DELETE_AUCTION,
    }
}
