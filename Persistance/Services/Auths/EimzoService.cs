using Application.Exceptions;
using Application.Interfaces.Auths;
using Application.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistance.Models.ConfigModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Persistance.Services.Auths
{
    /// <summary>
    /// Talks to the site's own E-IMZO-SERVER (https://github.com/qo0p/e-imzo-doc).
    /// Request bodies are raw Base64 text; /backend and timestamp calls carry the user's IP (X-Real-IP)
    /// and the site domain (Host).
    /// </summary>
    public class EimzoService(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        IOptions<EimzoConfiguration> options,
        ILogger<EimzoService> logger) : IEimzoService
    {
        // Subject name keys of E-IMZO certificates.
        private const string PinflKey = "1.2.860.3.16.1.2";
        private const string LegalInnKey = "1.2.860.3.16.1.1";
        private const string PersonInnKey = "UID";
        private const string CommonNameKey = "CN";
        private const string FirstNameKey = "Name";
        private const string LastNameKey = "SURNAME";
        private const string OrganizationKey = "O";

        private const int SuccessStatus = 1;
        // The certificate status could not be checked, usually because the E-IMZO VPN is down.
        private const int CertificateStatusUncheckedStatus = -1;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient = httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly EimzoConfiguration _configuration = options.Value;
        private readonly ILogger<EimzoService> _logger = logger;

        public async Task<EimzoChallengeResult> GetChallengeAsync(CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/frontend/challenge");
            using var json = await SendAsync(request, cancellationToken);
            var root = json.RootElement;

            var status = GetStatus(root);
            var challenge = GetString(root, "challenge");
            if (status != SuccessStatus || string.IsNullOrEmpty(challenge))
                throw Unavailable($"E-IMZO-SERVER could not create a challenge (status {status}).");

            return new EimzoChallengeResult(challenge, root.TryGetProperty("ttl", out var ttl) && ttl.TryGetInt32(out var seconds) ? seconds : 0);
        }

        public async Task<EimzoAuthResult> AuthenticateAsync(string signedChallengePkcs7, CancellationToken cancellationToken)
        {
            using var request = CreateSignedRequest("/backend/auth", signedChallengePkcs7);
            using var json = await SendAsync(request, cancellationToken);
            var root = json.RootElement;

            var status = GetStatus(root);
            ThrowIfCertificateStatusUnchecked(status);

            var certificate = root.TryGetProperty("subjectCertificateInfo", out var info) && info.ValueKind == JsonValueKind.Object
                ? ToCertificateInfo(info, ReadStringMap(info, "subjectName"))
                : null;

            return new EimzoAuthResult(status == SuccessStatus, status, DescribeStatus(status, GetString(root, "message")), certificate);
        }

        public async Task<string> TimestampAsync(string pkcs7Base64, CancellationToken cancellationToken)
        {
            using var request = CreateSignedRequest("/frontend/timestamp/pkcs7", pkcs7Base64);
            using var json = await SendAsync(request, cancellationToken);
            var root = json.RootElement;

            var status = GetStatus(root);
            var timestamped = GetString(root, "pkcs7b64");
            if (status != SuccessStatus || string.IsNullOrEmpty(timestamped))
                throw new BadRequestException($"The document could not be timestamped: {DescribeStatus(status, GetString(root, "message"))}");

            return timestamped;
        }

        public Task<EimzoDocumentVerificationResult> VerifyAttachedAsync(string pkcs7Base64, CancellationToken cancellationToken)
            => VerifyAsync("/backend/pkcs7/verify/attached", pkcs7Base64, cancellationToken);

        public Task<EimzoDocumentVerificationResult> VerifyDetachedAsync(string documentBase64, string pkcs7Base64, CancellationToken cancellationToken)
            => VerifyAsync("/backend/pkcs7/verify/detached", $"{documentBase64}|{pkcs7Base64}", cancellationToken);

        private async Task<EimzoDocumentVerificationResult> VerifyAsync(string path, string body, CancellationToken cancellationToken)
        {
            using var request = CreateSignedRequest(path, body);
            using var json = await SendAsync(request, cancellationToken);
            var root = json.RootElement;

            var status = GetStatus(root);
            ThrowIfCertificateStatusUnchecked(status);

            string? documentBase64 = null;
            var signers = new List<EimzoSignerInfo>();
            if (root.TryGetProperty("pkcs7Info", out var pkcs7Info) && pkcs7Info.ValueKind == JsonValueKind.Object)
            {
                documentBase64 = GetString(pkcs7Info, "documentBase64");
                if (pkcs7Info.TryGetProperty("signers", out var signerArray) && signerArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var signer in signerArray.EnumerateArray())
                        signers.Add(ToSignerInfo(signer));
                }
            }

            return new EimzoDocumentVerificationResult(
                status == SuccessStatus,
                status,
                DescribeStatus(status, GetString(root, "message")),
                documentBase64,
                signers,
                root.GetRawText());
        }

        /// <summary>
        /// Request for endpoints that need to know who is signing: the end user's IP and our site domain.
        /// </summary>
        private HttpRequestMessage CreateSignedRequest(string path, string body)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain")
            };
            request.Headers.Host = _configuration.SiteHost;
            request.Headers.TryAddWithoutValidation("X-Real-IP", GetClientIp());
            return request;
        }

        private string GetClientIp()
        {
            var address = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            if (address == null)
                return IPAddress.Loopback.ToString();

            return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
        }

        private async Task<JsonDocument> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw Unavailable("E-IMZO-SERVER is unreachable.", ex);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw Unavailable("E-IMZO-SERVER did not answer in time.", ex);
            }

            using (response)
            {
                if ((int)response.StatusCode >= 500)
                    throw Unavailable($"E-IMZO-SERVER failed with HTTP {(int)response.StatusCode}.");

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                try
                {
                    return JsonDocument.Parse(content);
                }
                catch (JsonException ex)
                {
                    // HTTP 400 is E-IMZO-SERVER's answer to malformed input (for example a broken Base64 signature).
                    if (response.StatusCode == HttpStatusCode.BadRequest)
                        throw new BadRequestException("The signed data is not valid.");

                    throw Unavailable("E-IMZO-SERVER returned an unreadable response.", ex);
                }
            }
        }

        private static void ThrowIfCertificateStatusUnchecked(int status)
        {
            if (status == CertificateStatusUncheckedStatus)
                throw new ServiceUnavailableException("Certificate status could not be checked. E-IMZO-SERVER has no connection to the E-IMZO VPN.");
        }

        private ServiceUnavailableException Unavailable(string message, Exception? innerException = null)
        {
            _logger.LogError(innerException, "{Message} BaseUrl: {BaseUrl}", message, _configuration.BaseUrl);
            return new ServiceUnavailableException("E-IMZO service is temporarily unavailable.", innerException);
        }

        private static EimzoSignerInfo ToSignerInfo(JsonElement signer)
        {
            EimzoCertificateInfo? certificate = null;
            // certificate[0] is the signer's own certificate, followed by the CA chain.
            if (signer.TryGetProperty("certificate", out var chain) && chain.ValueKind == JsonValueKind.Array && chain.GetArrayLength() > 0)
            {
                var own = chain[0];
                var subject = ReadStringMap(own, "subjectInfo") ?? ReadStringMap(own, "subjectName");
                certificate = ToCertificateInfo(own, subject);
            }

            string? timestampTime = null;
            if (signer.TryGetProperty("timeStampInfo", out var timestamp) && timestamp.ValueKind == JsonValueKind.Object)
                timestampTime = GetString(timestamp, "time");

            return new EimzoSignerInfo(
                certificate,
                GetBool(signer, "verified"),
                GetBool(signer, "certificateVerified"),
                GetString(signer, "signingTime"),
                timestampTime);
        }

        private static EimzoCertificateInfo ToCertificateInfo(JsonElement certificate, IReadOnlyDictionary<string, string>? subject)
        {
            string? Subject(string key) => subject != null && subject.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

            return new EimzoCertificateInfo
            {
                SerialNumber = GetString(certificate, "serialNumber"),
                Pinfl = Subject(PinflKey),
                PersonInn = Subject(PersonInnKey),
                LegalInn = Subject(LegalInnKey),
                CommonName = Subject(CommonNameKey),
                FirstName = Subject(FirstNameKey),
                LastName = Subject(LastNameKey),
                Organization = Subject(OrganizationKey),
                ValidFrom = ParseDate(GetString(certificate, "validFrom")),
                ValidTo = ParseDate(GetString(certificate, "validTo"))
            };
        }

        private static IReadOnlyDictionary<string, string>? ReadStringMap(JsonElement parent, string property)
        {
            if (!parent.TryGetProperty(property, out var map) || map.ValueKind != JsonValueKind.Object)
                return null;

            return map.EnumerateObject()
                      .Where(x => x.Value.ValueKind == JsonValueKind.String)
                      .ToDictionary(x => x.Name, x => x.Value.GetString()!, StringComparer.OrdinalIgnoreCase);
        }

        private static int GetStatus(JsonElement root)
            => root.TryGetProperty("status", out var status) && status.TryGetInt32(out var value) ? value : 0;

        private static string? GetString(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        private static bool GetBool(JsonElement element, string property)
            => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

        // Kept as returned by E-IMZO-SERVER (no time zone conversion); informational only.
        private static DateTime? ParseDate(string? value)
            => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

        private static string DescribeStatus(int status, string? serverMessage) => status switch
        {
            1 => "OK",
            -5 => "Signing time is outside the allowed window. Check the computer clock and sign again.",
            -10 => "The signature is invalid.",
            -11 => "The certificate is invalid.",
            -12 => "The certificate was not valid at the signing time.",
            -20 => "The challenge or timestamp was not found or has expired. Request a new one and sign again.",
            -21 => "The signature or timestamp hash is invalid.",
            -22 => "The timestamp certificate is invalid.",
            -23 => "The timestamp certificate was not valid at the signing time.",
            -24 => "The certificate policy is not allowed.",
            -25 => "The certificate authority is not allowed.",
            _ => string.IsNullOrWhiteSpace(serverMessage) ? $"E-IMZO verification failed (status {status})." : serverMessage
        };
    }
}
