namespace Infrastructure.Models.ConfigModels
{
    public sealed class PasswordResetConfiguration
    {
        public const string SectionName = "PasswordReset";

        /// <summary>
        /// Frontend page that reads <c>?token=</c> and posts it with the new password to <c>POST api/auth/reset-password</c>
        /// (e.g. "https://i-rieltor.uz/parolni-tiklash"). When empty, it is taken from
        /// <c>EmailConfirmation:LinkBaseUrl</c> by replacing its trailing "/confirm-email".
        /// </summary>
        public string? LinkBaseUrl { get; set; }

        [System.ComponentModel.DataAnnotations.Range(5, 1440)]
        public int TokenMinutes { get; set; } = 60;
    }
}
