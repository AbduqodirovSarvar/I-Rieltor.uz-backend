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

        public static bool IsSystemRole(string? roleName)
            => string.Equals(roleName, SuperAdmin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, DefaultUser, StringComparison.OrdinalIgnoreCase);
    }
}
