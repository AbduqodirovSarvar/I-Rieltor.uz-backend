using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services.Files
{
    /// <summary>
    /// Where uploaded files live on disk: <c>&lt;root&gt;/uploads</c> (served at /uploads) and
    /// <c>&lt;root&gt;/deleted</c> (never served). One place, so the file service and the static file middleware agree.
    /// </summary>
    public static class FileStorageLocation
    {
        public const string RootPathKey = "FileStorage:RootPath";
        private const string DefaultRootPath = "wwwroot";

        /// <summary>
        /// <c>FileStorage:RootPath</c> as an absolute path. It may be absolute (e.g. a Docker volume "/data/files")
        /// or relative to the content root; unset means "wwwroot" next to the app, as before.
        /// </summary>
        public static string ResolveRootPath(IConfiguration configuration, string contentRootPath)
        {
            var configured = configuration[RootPathKey];
            var path = string.IsNullOrWhiteSpace(configured) ? DefaultRootPath : configured;
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(contentRootPath, path));
        }

        public static string UploadsPath(string rootPath) => Path.Combine(rootPath, "uploads");
    }
}
