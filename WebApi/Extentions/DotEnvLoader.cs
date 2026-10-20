namespace WebApi.Extentions
{
    /// <summary>
    /// Loads KEY=VALUE pairs from a .env file into process environment variables,
    /// so they are picked up by the standard environment variables configuration provider
    /// (use "__" as the section separator, e.g. JWTConfiguration__Secret).
    /// Variables already set in the environment take precedence over the file.
    /// </summary>
    public static class DotEnvLoader
    {
        public static void Load(string fileName = ".env")
        {
            var path = FindFile(fileName);
            if (path == null)
                return;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                    value = value[1..^1];

                if (Environment.GetEnvironmentVariable(key) == null)
                    Environment.SetEnvironmentVariable(key, value);
            }
        }

        /// <summary>
        /// Searches the current directory and its parents, stopping at the solution root.
        /// </summary>
        private static string? FindFile(string fileName)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                    return candidate;

                if (directory.EnumerateFiles("*.sln").Any())
                    return null;

                directory = directory.Parent;
            }
            return null;
        }
    }
}
