using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// Building blocks of the auction tests: a published auction, users with E-IMZO keys, applications and approvals.
    /// Everything goes through the API the way the web and admin applications would use it.
    /// </summary>
    public sealed class AuctionScenario(ApiFactory api)
    {
        public sealed record Applicant(HttpClient Client, string UserId, string Pinfl, string ParticipantId);

        public static MultipartFormDataContent Form(params (string Name, string Value)[] fields)
        {
            var form = new MultipartFormDataContent();
            foreach (var (name, value) in fields)
                form.Add(new StringContent(value), name);
            return form;
        }

        public static string Iso(DateTime utc) => utc.ToString("O", CultureInfo.InvariantCulture);

        public static DateTime Time(JsonNode? node) => DateTime.Parse(node!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

        public static async Task<JsonObject> Ok(HttpResponseMessage response)
        {
            var body = await ApiFactory.JsonAsync(response);
            Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
            return body;
        }

        /// <summary>A published property in a fresh category.</summary>
        public async Task<string> PropertyAsync(HttpClient admin)
        {
            var key = Guid.NewGuid().ToString("N")[..8];
            var category = (await Ok(await admin.PostAsync("api/propertycategory", Form(("Name", "Toifa " + key)))))["data"]!["id"]!.GetValue<string>();
            return (await Ok(await admin.PostAsync("api/property", Form(("Title", "Obyekt " + key), ("CategoryId", category), ("Region", "Hudud-" + key), ("Address", "Manzil 1"), ("Status", "PUBLISHED")))))["data"]!["id"]!.GetValue<string>();
        }

        /// <summary>
        /// A SCHEDULED auction. Defaults: starts in a day, ends two hours later, applications close an hour before the start,
        /// 100 000 000 so'm to start with, 1 000 000 per step, 5 000 000 deposit.
        /// </summary>
        public async Task<JsonObject> PublishedAuctionAsync(HttpClient admin, DateTime? start = null, decimal deposit = 5_000_000m, DateTime? applicationDeadline = null,
            int extensionMinutes = 0, bool offerToRunnerUp = true, string? propertyId = null)
        {
            var startsAt = start ?? DateTime.UtcNow.AddDays(1);
            propertyId ??= await PropertyAsync(admin);
            var created = (await Ok(await admin.PostAsync("api/auction", Form(
                ("PropertyId", propertyId), ("StartingPrice", "100000000"), ("MinBidStep", "1000000"),
                ("DepositAmount", deposit.ToString(CultureInfo.InvariantCulture)),
                ("ApplicationDeadline", Iso(applicationDeadline ?? startsAt.AddHours(-1))), ("StartsAt", Iso(startsAt)), ("EndsAt", Iso(startsAt.AddHours(2))),
                ("ExtensionMinutes", extensionMinutes.ToString()), ("OfferToRunnerUp", offerToRunnerUp ? "true" : "false")))))["data"]!;
            var id = created["id"]!.GetValue<string>();
            return (await Ok(await admin.PostAsJsonAsync("api/auction/publish", new { id })))["data"]!.AsObject();
        }

        /// <summary>Signs up with an E-IMZO key and applies to the auction (the application stays PENDING).</summary>
        public async Task<Applicant> ApplyAsync(string auctionId, string? legalInn = null)
        {
            var (client, userId, pinfl) = await api.EimzoUserAsync(legalInn);
            return new Applicant(client, userId, pinfl, await ApplyAsync(client, auctionId, pinfl, legalInn));
        }

        public async Task<string> ApplyAsync(HttpClient client, string auctionId, string pinfl, string? legalInn = null)
        {
            var text = (await Ok(await client.GetAsync($"api/participation/application-text?AuctionId={auctionId}")))["data"]!["text"]!.GetValue<string>();
            var signed = FakeEimzoServer.SignDocument(text, legalInn == null ? pinfl : ApiFactory.NewPinfl(), legalInn);
            return (await Ok(await client.PostAsJsonAsync("api/participation/apply", new { auctionId, signedData = signed })))["data"]!["id"]!.GetValue<string>();
        }

        public async Task<JsonObject> ApproveAsync(HttpClient admin, string participantId)
            => (await Ok(await admin.PostAsJsonAsync("api/participation/approve", new { id = participantId })))["data"]!.AsObject();

        /// <summary>An approved participant whose deposit is paid (set directly: the payment flow has its own tests).</summary>
        public async Task<Applicant> ParticipantAsync(HttpClient admin, string auctionId, bool depositPaid = true)
        {
            var applicant = await ApplyAsync(auctionId);
            await ApproveAsync(admin, applicant.ParticipantId);
            if (depositPaid)
                await api.ScalarAsync<object>($"update \"AuctionParticipants\" set \"DepositStatus\" = 'PAID' where \"Id\" = '{applicant.ParticipantId}'");
            return applicant;
        }
    }
}
