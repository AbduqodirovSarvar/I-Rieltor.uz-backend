namespace Persistance.Models.ConfigModels
{
    public sealed class SmtpConfiguration
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = null!;
        [System.ComponentModel.DataAnnotations.Range(1, 65535)]
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FromAddress { get; set; } = null!;
        public string FromName { get; set; } = null!;
    }
}
