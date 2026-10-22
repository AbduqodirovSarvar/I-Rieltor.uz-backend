using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// In-process stand-in for E-IMZO-SERVER (request/response shapes of qo0p/e-imzo-doc), started on a free port.
    /// Signed data is faked as text, so a test chooses who "signed":
    /// <list type="bullet">
    /// <item><c>SIGNED|challenge|pinfl|legalInn|organisation</c> for sign-in;</item>
    /// <item><c>ATT|documentBase64|pinfl|legalInn</c> for an attached document signature (a timestamp appends <c>|TS</c>);</item>
    /// <item>mobile sessions: <see cref="Upload"/> plays the phone.</item>
    /// </list>
    /// </summary>
    public sealed class FakeEimzoServer : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly ConcurrentDictionary<string, byte> _challenges = new();
        private readonly ConcurrentDictionary<string, MobileSession> _sessions = new();

        private sealed record MobileSession(string Kind, string? Pinfl = null, string? LegalInn = null, string? Document = null, bool Uploaded = false);

        public string BaseUrl { get; private set; } = string.Empty;

        private FakeEimzoServer(WebApplication app) => _app = app;

        public static async Task<FakeEimzoServer> StartAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            var app = builder.Build();
            var server = new FakeEimzoServer(app);
            server.Map(app);
            await app.StartAsync();
            server.BaseUrl = app.Urls.First();
            return server;
        }

        // ---------------- test side

        public static string Sign(string challenge, string pinfl, string? legalInn = null, string? organisation = null)
            => $"SIGNED|{challenge}|{pinfl}|{legalInn}|{organisation}";

        public static string SignDocument(string documentText, string pinfl, string? legalInn = null)
            => $"ATT|{Convert.ToBase64String(Encoding.UTF8.GetBytes(documentText))}|{pinfl}|{legalInn}";

        /// <summary>The phone finishes a session: the key of <paramref name="pinfl"/> signs (<paramref name="document"/> for a signing session).</summary>
        public void Upload(string documentId, string pinfl, string? legalInn = null, string? document = null)
        {
            var existing = _sessions[documentId];
            _sessions[documentId] = existing with { Pinfl = pinfl, LegalInn = legalInn, Document = document ?? existing.Document, Uploaded = true };
        }

        // ---------------- server side

        private static JsonObject Subject(string pinfl, string? legalInn, string? organisation) => new()
        {
            ["CN"] = "VALIYEV ALI",
            ["Name"] = "ALI",
            ["SURNAME"] = "VALIYEV",
            ["1.2.860.3.16.1.2"] = pinfl,
            ["UID"] = "5" + pinfl[..8],
            ["O"] = organisation,
            ["1.2.860.3.16.1.1"] = legalInn,
        };

        private static JsonObject Certificate(string pinfl, string? legalInn, string? organisation) => new()
        {
            ["serialNumber"] = "7777aaaa",
            ["subjectName"] = Subject(pinfl, legalInn, organisation),
            ["validFrom"] = "2025-01-10 10:00:00",
            ["validTo"] = "2030-01-10 23:59:59",
        };

        private static string[] Parts(string body) => body.Split('|');
        private static string? Opt(string[] parts, int index) => index < parts.Length && parts[index].Length > 0 ? parts[index] : null;

        private void Map(WebApplication app)
        {
            app.MapGet("/frontend/challenge", () =>
            {
                var challenge = Guid.NewGuid().ToString();
                _challenges[challenge] = 0;
                return Results.Json(new { challenge, ttl = 120, status = 1, message = "" });
            });

            app.MapPost("/backend/auth", async (HttpRequest request) =>
            {
                var parts = Parts(await new StreamReader(request.Body).ReadToEndAsync());
                if (parts.Length < 3 || parts[0] != "SIGNED")
                    return Results.BadRequest();
                if (!_challenges.TryRemove(parts[1], out _))
                    return Results.Json(new { status = -20, message = "challenge not found" });
                return Results.Json(new JsonObject { ["status"] = 1, ["message"] = "", ["subjectCertificateInfo"] = Certificate(parts[2], Opt(parts, 3), Opt(parts, 4)) });
            });

            app.MapPost("/frontend/timestamp/pkcs7", async (HttpRequest request) =>
            {
                var body = await new StreamReader(request.Body).ReadToEndAsync();
                return body.StartsWith("ATT|")
                    ? Results.Json(new { status = 1, message = "", pkcs7b64 = body.EndsWith("|TS") ? body : body + "|TS" })
                    : Results.BadRequest();
            });

            app.MapPost("/backend/pkcs7/verify/attached", async (HttpRequest request) =>
            {
                var parts = Parts(await new StreamReader(request.Body).ReadToEndAsync());
                if (parts.Length < 3 || parts[0] != "ATT")
                    return Results.BadRequest();
                var timestamped = parts[^1] == "TS";
                var signer = new JsonObject
                {
                    ["signingTime"] = "2026-10-20 12:00:00",
                    ["verified"] = true,
                    ["certificateVerified"] = true,
                    ["certificate"] = new JsonArray(new JsonObject { ["serialNumber"] = "7777aaaa", ["subjectInfo"] = Subject(parts[2], Opt(parts, 3), Opt(parts, 3) == null ? null : "ORG MCHJ") }),
                };
                if (timestamped)
                    signer["timeStampInfo"] = new JsonObject { ["time"] = "2026-10-20 12:00:05" };
                return Results.Json(new JsonObject
                {
                    ["status"] = 1,
                    ["message"] = "",
                    ["pkcs7Info"] = new JsonObject { ["documentBase64"] = parts[1], ["signers"] = new JsonArray(signer) },
                });
            });

            // ---- E-IMZO Mobile
            string Start(string kind)
            {
                var id = Guid.NewGuid().ToString("N");
                _sessions[id] = new MobileSession(kind);
                return id;
            }
            app.MapPost("/frontend/mobile/auth", () => Results.Json(new { status = 1, siteId = "TESTSITE", documentId = Start("auth"), challange = Guid.NewGuid().ToString("N") }));
            app.MapPost("/frontend/mobile/sign", () => Results.Json(new { status = 1, siteId = "TESTSITE", documentId = Start("sign") }));
            app.MapPost("/frontend/mobile/status", async (HttpRequest request) =>
            {
                var form = await request.ReadFormAsync();
                if (!_sessions.TryGetValue(form["documentId"].ToString(), out var session))
                    return Results.Json(new { status = -2, message = "not found" });
                return Results.Json(new { status = session.Uploaded ? 1 : 2 });
            });
            app.MapGet("/backend/mobile/authenticate/{id}", (string id) =>
            {
                if (!_sessions.TryGetValue(id, out var session))
                    return Results.Json(new { status = -2, message = "not found" });
                if (!session.Uploaded || session.Kind != "auth")
                    return Results.Json(new { status = -4, message = "no signature" });
                return Results.Json(new JsonObject { ["status"] = 1, ["subjectCertificateInfo"] = Certificate(session.Pinfl!, session.LegalInn, session.LegalInn == null ? null : "ORG MCHJ") });
            });
            app.MapPost("/backend/mobile/verify", async (HttpRequest request) =>
            {
                var form = await request.ReadFormAsync();
                if (!_sessions.TryGetValue(form["documentId"].ToString(), out var session))
                    return Results.Json(new { status = -2, message = "not found" });
                if (!session.Uploaded || session.Kind != "sign")
                    return Results.Json(new { status = -4, message = "no signature" });
                // The phone signed this document; a different one does not match its digest.
                if (session.Document != null && Convert.ToBase64String(Encoding.UTF8.GetBytes(session.Document)) != form["document"].ToString())
                    return Results.Json(new { status = -99, message = "Calculated digest does not match" });
                return Results.Json(new JsonObject
                {
                    ["status"] = 1,
                    ["subjectCertificateInfo"] = Certificate(session.Pinfl!, session.LegalInn, session.LegalInn == null ? null : "ORG MCHJ"),
                    ["verificationInfo"] = new JsonObject { ["signingTime"] = "2026-10-20 12:00:00", ["timestampedTime"] = "2026-10-20 12:00:05" },
                    ["pkcs7Attached"] = "PKCS7:" + form["documentId"],
                });
            });
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
