namespace Domain.Constants
{
    /// <summary>
    /// Tizim ishlashi uchun zarur rollar. Seeder ularni yaratadi, ularni o'chirib yoki nomini o'zgartirib bo'lmaydi.
    /// </summary>
    public static class SystemRoles
    {
        /// <summary>
        /// Barcha ruxsatlarga ega administrator; seeder har startda unga yetishmagan ruxsatlarni qo'shadi.
        /// </summary>
        public const string SuperAdmin = "SUPER_ADMIN";

        /// <summary>
        /// Ro'yxatdan o'tgan har bir yangi foydalanuvchiga beriladigan rol.
        /// </summary>
        public const string DefaultUser = "DEFAULT_USER";

        /// <summary>
        /// Login of the account the seeder creates. Reserved in any letter case: nobody else may register or take it,
        /// so a login can never be mistaken for the system administrator.
        /// </summary>
        public const string BootstrapAdminLogin = "SUPER_ADMIN";

        public static bool IsReservedLogin(string? login)
            => login != null && login.Trim().StartsWith(BootstrapAdminLogin, StringComparison.OrdinalIgnoreCase);

        public static bool IsSystemRole(string? roleName)
            => string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, DefaultUser, StringComparison.OrdinalIgnoreCase);
    }
}
