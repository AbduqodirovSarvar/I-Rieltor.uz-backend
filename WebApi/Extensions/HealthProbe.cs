namespace WebApi.Extensions
{
    /// <summary>
    /// <c>dotnet WebApi.dll --healthcheck</c>: asks the running API in the same container for <c>/health/ready</c>
    /// and exits with 0 when it answers 2xx (Healthy or Degraded), otherwise 1. Used by the compose healthcheck.
    /// </summary>
    public static class HealthProbe
    {
        public const string Argument = "--healthcheck";

        public static async Task<int> RunAsync()
        {
            var port = (Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080")
                .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault() ?? "8080";

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{port}/health/ready");
            // Host filtering (AllowedHosts) would refuse "127.0.0.1"; send a host it accepts.
            request.Headers.Host = AllowedHost();
            try
            {
                using var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode ? 0 : 1;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return 1;
            }
        }

        private static string AllowedHost()
        {
            var host = (Environment.GetEnvironmentVariable("AllowedHosts") ?? string.Empty)
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault(h => h != "*");
            if (host == null)
                return "localhost";
            // "*.example.com" accepts any subdomain.
            return host.StartsWith("*.", StringComparison.Ordinal) ? "health" + host[1..] : host;
        }
    }
}
