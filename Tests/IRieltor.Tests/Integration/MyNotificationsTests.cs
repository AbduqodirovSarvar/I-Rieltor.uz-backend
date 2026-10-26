using Application.Interfaces.Notifications;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace IRieltor.Tests.Integration
{
    /// <summary>W-B01: a user reads their own messages; nobody sees anybody else's.</summary>
    [Collection(ApiCollection.Name)]
    public class MyNotificationsTests(ApiFactory api)
    {
        private static int _phone = 906_000_000;

        private async Task<(Guid Id, HttpClient Client)> UserAsync(string login)
        {
            await api.SignUpAsync(login);
            var phone = "+998" + Interlocked.Increment(ref _phone);
            await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '{phone}', \"EmailConfirmedAt\" = now() where \"Login\" = '{login}'");
            var id = await api.ScalarAsync<Guid>($"select \"Id\" from \"Users\" where \"Login\" = '{login}'");
            return (id, api.Authorized(await api.SignInAsync(login, "Strong@123")));
        }

        private async Task SendAsync(Guid userId, string lot)
        {
            using var scope = api.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<INotificationService>().NotifyAsync(userId, NotificationType.APPLICATION_APPROVED,
                new Dictionary<string, string> { ["lot"] = lot, ["number"] = "7", ["deposit"] = "5 000 000", ["start"] = "21.10.2026 14:00" }, null, false);
            await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().DispatchDueAsync(DateTime.UtcNow);
        }

        [Fact]
        public async Task A_user_sees_their_own_messages_once_per_event_with_the_email_text_and_nobody_elses()
        {
            var ali = await UserAsync("mynot_a1");
            var vali = await UserAsync("mynot_a2");
            await SendAsync(ali.Id, "2026-000501");
            await SendAsync(vali.Id, "2026-000502");

            var list = (await ApiFactory.JsonAsync(await ali.Client.GetAsync("api/notification/my")))["data"]!.AsArray();

            // An e-mail and an SMS were sent for the one event: one row, the e-mail's.
            var row = Assert.Single(list);
            Assert.Equal("APPLICATION_APPROVED", row!["type"]!.GetValue<string>());
            Assert.Equal("Ariza tasdiqlandi", row["subject"]!.GetValue<string>());
            Assert.Contains("2026-000501", row["body"]!.GetValue<string>());
            Assert.DoesNotContain("2026-000502", list.ToJsonString());
        }

        [Fact]
        public async Task Newest_first_and_a_message_that_was_not_sent_is_not_shown()
        {
            var user = await UserAsync("mynot_b1");
            await SendAsync(user.Id, "2026-000601");
            await Task.Delay(1100);
            await SendAsync(user.Id, "2026-000602");
            await api.ScalarAsync<object>($"insert into \"Notifications\" (\"Id\", \"CreatedAt\", \"IsDeleted\", \"UserId\", \"Channel\", \"Type\", \"Status\", \"Destination\", \"Body\", \"Attempts\", \"NextAttemptAt\") " +
                                          $"values (gen_random_uuid(), now() + interval '1 hour', false, '{user.Id}', 'EMAIL', 'OUTBID', 'FAILED', 'x@y.z', 'must not appear', 1, now())");

            var list = (await ApiFactory.JsonAsync(await user.Client.GetAsync("api/notification/my")))["data"]!.AsArray();

            Assert.Equal(2, list.Count);
            Assert.Contains("2026-000602", list[0]!["body"]!.GetValue<string>());
            Assert.Contains("2026-000601", list[1]!["body"]!.GetValue<string>());
            Assert.DoesNotContain("must not appear", list.ToJsonString());
        }

        [Fact]
        public async Task Without_signing_in_it_is_401_and_the_answer_is_never_cached()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/notification/my")).StatusCode);
            var user = await UserAsync("mynot_c1");
            var response = await user.Client.GetAsync("api/notification/my");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        }
    }
}
