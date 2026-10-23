using Application.Interfaces.Auctions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using static IRieltor.Tests.Integration.AuctionScenario;

namespace IRieltor.Tests.Integration
{
    /// <summary>P2-04: applying with an E-IMZO signature, the staff's review, the deposit state, the cabinet and the clock.</summary>
    [Collection(ApiCollection.Name)]
    public class ParticipationTests(ApiFactory api)
    {
        private readonly AuctionScenario _scenario = new(api);
        private static int _phone = 902_000_000;

        private async Task<AuctionLifecycleRun> RunAsync(DateTime now)
        {
            using var scope = api.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<IAuctionLifecycleService>().RunOnceAsync(now);
        }

        private async Task<string> StatusOfAsync(string participantId) => await api.ScalarAsync<string>($"select \"Status\" from \"AuctionParticipants\" where \"Id\" = '{participantId}'") ?? "";

        [Fact]
        public async Task An_applicant_signs_the_text_the_staff_approve_and_the_numbers_follow_the_approvals()
        {
            var admin = await api.AdminClientAsync();
            var auction = await _scenario.PublishedAuctionAsync(admin);
            var id = auction["id"]!.GetValue<string>();
            var (client, _, pinfl) = await api.EimzoUserAsync();

            // The text names the lot, the terms and the applicant's own PINFL.
            var text = (await Ok(await client.GetAsync($"api/participation/application-text?AuctionId={id}")))["data"]!["text"]!.GetValue<string>();
            Assert.Contains(auction["lotNumber"]!.GetValue<string>(), text);
            Assert.Contains("Zakalat: 5 000 000 so'm", text);
            Assert.Contains($"PINFL: {pinfl}", text);

            var applied = (await Ok(await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(text, pinfl) })))["data"]!;
            var participantId = applied["id"]!.GetValue<string>();
            Assert.Equal("PENDING", applied["status"]!.GetValue<string>());
            Assert.Null(applied["participantNumber"]?.GetValue<int?>());
            // The evidence is kept: the timestamped signature and who signed.
            Assert.Contains("|TS", await api.ScalarAsync<string>($"select \"SignedApplication\" from \"AuctionParticipants\" where \"Id\" = '{participantId}'") ?? "");
            Assert.Equal(pinfl, await api.ScalarAsync<string>($"select \"SignerIdentifier\" from \"AuctionParticipants\" where \"Id\" = '{participantId}'"));

            // Once only.
            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(text, pinfl) })).StatusCode);

            var approved = await _scenario.ApproveAsync(admin, participantId);
            Assert.Equal("APPROVED", approved["status"]!.GetValue<string>());
            Assert.Equal(1, approved["participantNumber"]!.GetValue<int>());
            Assert.Equal("NOT_PAID", approved["depositStatus"]!.GetValue<string>());
            Assert.Equal(pinfl, approved["userIdentifier"]!.GetValue<string>());
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/participation/approve", new { id = participantId })).StatusCode);

            var second = await _scenario.ApplyAsync(id);
            Assert.Equal(2, (await _scenario.ApproveAsync(admin, second.ParticipantId))["participantNumber"]!.GetValue<int>());

            var mine = (await Ok(await client.GetAsync("api/participation/my")))["data"]!.AsArray();
            var line = Assert.Single(mine, m => m!["auctionId"]!.GetValue<string>() == id)!;
            Assert.Equal("APPROVED", line["status"]!.GetValue<string>());
            Assert.Equal(1, line["participantNumber"]!.GetValue<int>());
            Assert.Equal(5_000_000m, line["depositAmount"]!.GetValue<decimal>());
        }

        [Fact]
        public async Task A_zero_deposit_counts_as_paid_at_approval()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin, deposit: 0))["id"]!.GetValue<string>();
            var applicant = await _scenario.ApplyAsync(id);

            Assert.Equal("PAID", (await _scenario.ApproveAsync(admin, applicant.ParticipantId))["depositStatus"]!.GetValue<string>());
        }

        [Fact]
        public async Task A_password_account_must_link_its_key_first_and_then_may_apply()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            await api.SignUpAsync("part_pwd1");
            var client = api.Authorized(await api.SignInAsync("part_pwd1", "Strong@123"));

            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"api/participation/application-text?AuctionId={id}")).StatusCode);

            var pinfl = ApiFactory.NewPinfl();
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) })).StatusCode);
            await _scenario.ApplyAsync(client, id, pinfl);

            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"api/participation/application-text?AuctionId={Guid.NewGuid()}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync($"api/participation/application-text?AuctionId={id}")).StatusCode);
        }

        [Fact]
        public async Task A_signature_must_cover_the_exact_text_and_belong_to_the_applicants_own_key()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var other = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var (client, _, pinfl) = await api.EimzoUserAsync();
            string TextOf(string auction) => Ok(client.GetAsync($"api/participation/application-text?AuctionId={auction}").Result).Result["data"]!["text"]!.GetValue<string>();

            // Another auction's text, an edited text, someone else's key, nonsense.
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(TextOf(other), pinfl) })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(TextOf(id).Replace("Zakalat: 5 000 000", "Zakalat: 0"), pinfl) })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(TextOf(id), ApiFactory.NewPinfl()) })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = "not a pkcs7" })).StatusCode);
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuctionParticipants\" where \"AuctionId\" = '{id}'"));

            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(TextOf(id), pinfl) })).StatusCode);
        }

        [Fact]
        public async Task A_legal_entity_applies_with_its_organisation_key()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var inn = "30" + Random.Shared.Next(1_000_000, 9_999_999);
            var (client, _, identifier) = await api.EimzoUserAsync(legalInn: inn);

            var text = (await Ok(await client.GetAsync($"api/participation/application-text?AuctionId={id}")))["data"]!["text"]!.GetValue<string>();
            Assert.Contains($"INN: {inn}", text);   // an organisation's account is identified by its INN
            Assert.Equal(inn, identifier);

            var signed = FakeEimzoServer.SignDocument(text, ApiFactory.NewPinfl(), legalInn: inn);   // the employee's own PINFL is not the account's
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = signed })).StatusCode);
        }

        [Fact]
        public async Task Applications_close_at_the_deadline()
        {
            var admin = await api.AdminClientAsync();
            var start = DateTime.UtcNow.AddHours(2);
            var id = (await _scenario.PublishedAuctionAsync(admin, start: start, applicationDeadline: DateTime.UtcNow.AddSeconds(3)))["id"]!.GetValue<string>();
            var (client, _, pinfl) = await api.EimzoUserAsync();
            var text = (await Ok(await client.GetAsync($"api/participation/application-text?AuctionId={id}")))["data"]!["text"]!.GetValue<string>();

            await Task.Delay(TimeSpan.FromSeconds(4));

            Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(text, pinfl) })).StatusCode);
        }

        [Fact]
        public async Task A_rejection_needs_a_reason_the_applicant_sees_and_is_final()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var applicant = await _scenario.ApplyAsync(id);

            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/participation/reject", new { id = applicant.ParticipantId, reason = "" })).StatusCode);
            var rejected = (await Ok(await admin.PostAsJsonAsync("api/participation/reject", new { id = applicant.ParticipantId, reason = "  Hujjatlar to'liq emas  " })))["data"]!;
            Assert.Equal("REJECTED", rejected["status"]!.GetValue<string>());

            var line = Assert.Single((await Ok(await applicant.Client.GetAsync("api/participation/my")))["data"]!.AsArray())!;
            Assert.Equal("Hujjatlar to'liq emas", line["rejectReason"]!.GetValue<string>());
            // A rejected applicant cannot simply apply again, withdraw, or be approved.
            var text = (await Ok(await applicant.Client.GetAsync($"api/participation/application-text?AuctionId={id}")))["data"]!["text"]!.GetValue<string>();
            Assert.Equal(HttpStatusCode.Conflict, (await applicant.Client.PostAsJsonAsync("api/participation/apply", new { auctionId = id, signedData = FakeEimzoServer.SignDocument(text, applicant.Pinfl) })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await applicant.Client.PostAsJsonAsync("api/participation/withdraw", new { auctionId = id })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/participation/approve", new { id = applicant.ParticipantId })).StatusCode);
        }

        [Fact]
        public async Task A_withdrawn_application_can_be_made_again_and_a_paid_deposit_is_queued_for_refund()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var applicant = await _scenario.ParticipantAsync(admin, id, depositPaid: true);

            var withdrawn = (await Ok(await applicant.Client.PostAsJsonAsync("api/participation/withdraw", new { auctionId = id })))["data"]!;
            Assert.Equal("WITHDRAWN", withdrawn["status"]!.GetValue<string>());
            // The deposit comes back through a refund request for the staff; twice is still one request.
            var refund = $"select count(*) from \"Payments\" where \"ParticipantId\" = '{applicant.ParticipantId}' and \"Purpose\" = 'REFUND' and \"Status\" = 'PENDING' and \"Amount\" = 5000000";
            Assert.Equal(1L, await api.ScalarAsync<long>(refund));
            Assert.Equal(HttpStatusCode.NotFound, (await applicant.Client.PostAsJsonAsync("api/participation/withdraw", new { auctionId = id })).StatusCode);

            var again = await _scenario.ApplyAsync(applicant.Client, id, applicant.Pinfl);
            Assert.NotEqual(applicant.ParticipantId, again);
            Assert.Equal("PENDING", await StatusOfAsync(again));
        }

        [Fact]
        public async Task Rejecting_an_approved_participant_with_a_paid_deposit_queues_the_refund()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var applicant = await _scenario.ParticipantAsync(admin, id, depositPaid: true);

            await Ok(await admin.PostAsJsonAsync("api/participation/reject", new { id = applicant.ParticipantId, reason = "Shartlar buzildi" }));

            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"Payments\" where \"ParticipantId\" = '{applicant.ParticipantId}' and \"Purpose\" = 'REFUND' and \"Status\" = 'PENDING'"));
        }

        [Fact]
        public async Task The_clock_closes_unreviewed_applications_at_the_start_and_reminds_the_approved_once()
        {
            var admin = await api.AdminClientAsync();
            var auction = await _scenario.PublishedAuctionAsync(admin);
            var id = auction["id"]!.GetValue<string>();
            var startsAt = Time(auction["startsAt"]);

            // One approved user who can be reached by SMS, one still pending.
            var approved = await _scenario.ApplyAsync(id);
            var phone = "+998" + Interlocked.Increment(ref _phone);
            await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '{phone}' where \"Id\" = '{approved.UserId}'");
            await _scenario.ApproveAsync(admin, approved.ParticipantId);
            var pending = await _scenario.ApplyAsync(id);

            // Far from the start: no reminder yet. 30 minutes before: one reminder, and only once.
            await RunAsync(startsAt.AddHours(-5));
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"AuctionId\" = '{id}' and \"Type\" = 'AUCTION_STARTING_SOON'"));
            var run = await RunAsync(startsAt.AddMinutes(-30));
            Assert.True(run.Reminded >= 1);
            await RunAsync(startsAt.AddMinutes(-20));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"AuctionId\" = '{id}' and \"Type\" = 'AUCTION_STARTING_SOON' and \"UserId\" = '{approved.UserId}'"));

            await RunAsync(startsAt.AddSeconds(1));
            Assert.Equal("APPROVED", await StatusOfAsync(approved.ParticipantId));
            Assert.Equal("REJECTED", await StatusOfAsync(pending.ParticipantId));
            Assert.Contains("ko'rib chiqilmadi", await api.ScalarAsync<string>($"select \"RejectReason\" from \"AuctionParticipants\" where \"Id\" = '{pending.ParticipantId}'") ?? "");
            // After the start nobody can be reviewed or withdraw any more.
            Assert.Equal(HttpStatusCode.Conflict, (await approved.Client.PostAsJsonAsync("api/participation/withdraw", new { auctionId = id })).StatusCode);
        }

        [Fact]
        public async Task The_participant_is_told_by_email_and_sms_when_approved()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var applicant = await _scenario.ApplyAsync(id);
            var phone = "+998" + Interlocked.Increment(ref _phone);
            await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '{phone}', \"Email\" = 'part_{applicant.UserId:N}@tests.local', \"EmailConfirmedAt\" = now() where \"Id\" = '{applicant.UserId}'");

            await _scenario.ApproveAsync(admin, applicant.ParticipantId);
            using (var scope = api.Services.CreateScope())
                await scope.ServiceProvider.GetRequiredService<Application.Interfaces.Notifications.INotificationDispatcher>().DispatchDueAsync(DateTime.UtcNow.AddSeconds(2));

            var sms = Assert.Single(api.Sinks.SmsTo(phone));
            Assert.Contains("arizangiz tasdiqlandi", sms.Text);
            Assert.Contains("Ishtirokchi raqamingiz: 1", sms.Text);
            Assert.Contains("Zakalat: 5 000 000 so'm", sms.Text);
            Assert.Single(api.Sinks.EmailsTo($"part_{applicant.UserId:N}@tests.local").Where(e => e.Subject == "Ariza tasdiqlandi"));
        }

        [Fact]
        public async Task An_application_can_be_signed_on_the_phone()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var (client, _, pinfl) = await api.EimzoUserAsync();
            var text = (await Ok(await client.GetAsync($"api/participation/application-text?AuctionId={id}")))["data"]!["text"]!.GetValue<string>();
            async Task<string> SessionAsync() => (await Ok(await client.PostAsync("api/auth/eimzo/mobile/sign/start", null)))["data"]!["documentId"]!.GetValue<string>();

            // The phone has not signed yet.
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/participation/apply-mobile", new { auctionId = id, documentId = await SessionAsync() })).StatusCode);
            // The phone signed some other text.
            var wrong = await SessionAsync();
            api.Eimzo.Upload(wrong, pinfl, document: "something else");
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("api/participation/apply-mobile", new { auctionId = id, documentId = wrong })).StatusCode);
            // Someone else's key signed the right text.
            var foreign = await SessionAsync();
            api.Eimzo.Upload(foreign, ApiFactory.NewPinfl(), document: text);
            Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("api/participation/apply-mobile", new { auctionId = id, documentId = foreign })).StatusCode);

            var good = await SessionAsync();
            api.Eimzo.Upload(good, pinfl, document: text);
            var applied = await client.PostAsJsonAsync("api/participation/apply-mobile", new { auctionId = id, documentId = good });
            Assert.Equal(HttpStatusCode.OK, applied.StatusCode);
            Assert.Equal("PENDING", (await ApiFactory.JsonAsync(applied))["data"]!["status"]!.GetValue<string>());
        }

        [Fact]
        public async Task The_staff_list_is_filtered_and_protected()
        {
            var admin = await api.AdminClientAsync();
            var id = (await _scenario.PublishedAuctionAsync(admin))["id"]!.GetValue<string>();
            var first = await _scenario.ApplyAsync(id);
            await _scenario.ApplyAsync(id);
            await _scenario.ApproveAsync(admin, first.ParticipantId);

            var all = (await Ok(await admin.GetAsync($"api/participation/list?Filters%5BauctionId.equals%5D={id}")))["data"]!.AsArray();
            Assert.Equal(2, all.Count);
            var pendingOnly = (await Ok(await admin.GetAsync($"api/participation/list?Filters%5BauctionId.equals%5D={id}&Filters%5Bstatus.equals%5D=PENDING")))["data"]!.AsArray();
            Assert.Single(pendingOnly);
            Assert.False(string.IsNullOrEmpty(all[0]!["userFullName"]!.GetValue<string>()));
            Assert.False(string.IsNullOrEmpty(all[0]!["lotNumber"]!.GetValue<string>()));   // the lot, not only its id

            // The text is searched in the applicant: name, PINFL/INN, phone, e-mail.
            var name = all.First(x => x!["userId"]!.GetValue<string>() == first.UserId.ToString())!["userFullName"]!.GetValue<string>();
            var byName = (await Ok(await admin.GetAsync($"api/participation/list?Filters%5BauctionId.equals%5D={id}&SearchText={Uri.EscapeDataString(name)}")))["data"]!.AsArray();
            Assert.Contains(byName, x => x!["userId"]!.GetValue<string>() == first.UserId.ToString());
            Assert.Empty((await Ok(await admin.GetAsync($"api/participation/list?Filters%5BauctionId.equals%5D={id}&SearchText=zzzz-nobody")))["data"]!.AsArray());

            // The auction page counts the applications for the staff.
            var counts = (await Ok(await admin.GetAsync($"api/auction?Id={id}")))["data"]!["participantCounts"]!;
            Assert.Equal(2, counts["total"]!.GetValue<int>());
            Assert.Equal(1, counts["pending"]!.GetValue<int>());
            Assert.Equal(1, counts["approved"]!.GetValue<int>());
            Assert.Equal(0, counts["depositPaid"]!.GetValue<int>());

            // The signature can be downloaded by the staff as a file (the text that was signed is inside), not by the applicant.
            var download = await admin.GetAsync($"api/participation/signed-application?Id={first.ParticipantId}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("application/pkcs7-signature", download.Content.Headers.ContentType!.MediaType);
            Assert.Contains(".p7s", download.Content.Headers.ContentDisposition!.FileName);
            Assert.Contains("|TS", System.Text.Encoding.UTF8.GetString(await download.Content.ReadAsByteArrayAsync()));
            Assert.Equal(HttpStatusCode.Forbidden, (await first.Client.GetAsync($"api/participation/signed-application?Id={first.ParticipantId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"api/participation/signed-application?Id={Guid.NewGuid()}")).StatusCode);

            Assert.Equal(HttpStatusCode.Forbidden, (await first.Client.GetAsync("api/participation/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await first.Client.PostAsJsonAsync("api/participation/approve", new { id = first.ParticipantId })).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/participation/my")).StatusCode);
        }
    }
}
