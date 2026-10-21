namespace Persistance.Models.ConfigModels
{
    public sealed class EmailConfirmationConfiguration
    {
        public const string SectionName = "EmailConfirmation";

        /// <summary>
        /// Frontend page that reads <c>?token=</c> and posts it to <c>POST api/auth/confirm-email</c>
        /// (e.g. "https://i-rieltor.uz/confirm-email"). The link does not hit the API directly: mail scanners
        /// open links in emails, and a GET that changes data would be triggered by them.
        /// </summary>
        public string LinkBaseUrl { get; set; } = null!;

        [System.ComponentModel.DataAnnotations.Range(1, 168)]
        public int TokenHours { get; set; } = 24;
    }
}
