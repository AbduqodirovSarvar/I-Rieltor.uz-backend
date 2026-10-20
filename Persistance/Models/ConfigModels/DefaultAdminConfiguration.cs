namespace Persistance.Models.ConfigModels
{
    public sealed class DefaultAdminConfiguration
    {
        public const string SectionName = "DefaultAdmin";

        public string Email { get; set; } = "admin@example.com";
        public string Password { get; set; } = null!;
    }
}
