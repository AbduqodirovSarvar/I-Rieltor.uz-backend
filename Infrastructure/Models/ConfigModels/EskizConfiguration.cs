namespace Infrastructure.Models.ConfigModels
{
    /// <summary>
    /// SMS through Eskiz (notify.eskiz.uz). Empty Email or Password: SMS is off.
    /// </summary>
    public sealed class EskizConfiguration
    {
        public const string SectionName = "Eskiz";

        public string BaseUrl { get; set; } = "https://notify.eskiz.uz";
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Sender name registered with Eskiz ("4546" is their shared test sender).
        /// </summary>
        public string From { get; set; } = "4546";

        [System.ComponentModel.DataAnnotations.Range(1, 120)]
        public int TimeoutSeconds { get; set; } = 15;
    }
}
