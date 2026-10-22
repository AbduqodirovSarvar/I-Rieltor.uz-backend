using Application.Exceptions;
using Application.Interfaces.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Persistance.Services.Notifications
{
    /// <summary>
    /// Eskiz REST API: <c>POST /api/auth/login</c> (email + password → token, valid about 30 days) and
    /// <c>POST /api/message/sms/send</c> (Bearer token; mobile_phone without "+", message, from).
    /// The token is kept in memory and renewed when Eskiz answers 401.
    /// </summary>
    public sealed class EskizSmsSender(HttpClient httpClient, IOptions<EskizConfiguration> options, EskizTokenStore tokenStore, ILogger<EskizSmsSender> logger) : ISmsSender
    {
        private readonly EskizConfiguration _configuration = options.Value;

        public bool IsEnabled => !string.IsNullOrWhiteSpace(_configuration.Email) && !string.IsNullOrWhiteSpace(_configuration.Password);

        public async Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
                throw new ServiceUnavailableException("SMS is not configured.");
            var number = phone.TrimStart('+');
            if (number.Length is < 9 or > 15 || !number.All(char.IsAsciiDigit))
                throw new BadRequestException("The phone number is not valid.");

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var token = await GetTokenAsync(forceNew: attempt > 0, cancellationToken);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/message/sms/send")
                {
                    Content = new MultipartFormDataContent
                    {
                        { new StringContent(number), "mobile_phone" },
                        { new StringContent(message), "message" },
                        { new StringContent(_configuration.From), "from" },
                    }
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await SendAsync(request, cancellationToken);
                if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
                    continue;   // the token expired: log in again once
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Eskiz answered {Status} to an SMS", (int)response.StatusCode);
                    throw new ServiceUnavailableException("The SMS provider did not accept the message.");
                }
                return;
            }
            throw new ServiceUnavailableException("The SMS provider rejected our login.");
        }

        private async Task<string> GetTokenAsync(bool forceNew, CancellationToken cancellationToken)
        {
            await tokenStore.Lock.WaitAsync(cancellationToken);
            try
            {
                if (tokenStore.Token != null && !forceNew)
                    return tokenStore.Token;

                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
                {
                    Content = new MultipartFormDataContent
                    {
                        { new StringContent(_configuration.Email), "email" },
                        { new StringContent(_configuration.Password), "password" },
                    }
                };
                using var response = await SendAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Eskiz login failed with {Status}", (int)response.StatusCode);
                    throw new ServiceUnavailableException("The SMS provider rejected our login.");
                }
                try
                {
                    using var json = JsonDocument.Parse(content);
                    tokenStore.Token = json.RootElement.GetProperty("data").GetProperty("token").GetString();
                }
                catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
                {
                    throw new ServiceUnavailableException("The SMS provider returned an unreadable login answer.", ex);
                }
                return tokenStore.Token ?? throw new ServiceUnavailableException("The SMS provider returned no token.");
            }
            finally
            {
                tokenStore.Lock.Release();
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new ServiceUnavailableException("The SMS provider is unreachable.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ServiceUnavailableException("The SMS provider did not answer in time.", ex);
            }
        }
    }

    /// <summary>
    /// The Eskiz login token, shared by all senders in the process (one login serves every request for about 30 days).
    /// </summary>
    public sealed class EskizTokenStore
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);
        public string? Token { get; set; }
    }
}
