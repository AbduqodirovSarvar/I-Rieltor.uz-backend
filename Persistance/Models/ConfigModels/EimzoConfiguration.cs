namespace Persistance.Models.ConfigModels
{
    public sealed class EimzoConfiguration
    {
        public const string SectionName = "Eimzo";

        /// <summary>
        /// Address of the site's own E-IMZO-SERVER inside the private network (for example "http://e-imzo-server:8080").
        /// Its /backend endpoints must never be reachable from the internet.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Url]
        public string BaseUrl { get; set; } = "http://localhost:8080";

        /// <summary>
        /// Site domain sent as the Host header; must be the domain registered for the E-IMZO API-KEY.
        /// </summary>
        public string SiteHost { get; set; } = "localhost";

        [System.ComponentModel.DataAnnotations.Range(1, 120)]
        public int TimeoutSeconds { get; set; } = 15;
    }
}
