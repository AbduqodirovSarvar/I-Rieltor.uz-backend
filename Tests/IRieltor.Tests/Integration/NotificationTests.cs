using Application.Interfaces.Notifications;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-08: the outbox (what is queued for whom), the dispatcher (sending, retrying, giving up) and the delivery log.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class NotificationTests(ApiFactory api)
    {
        private static int _phone = 901_000_000;

        /// <summary>A user with a unique phone, and optionally a confirmed email.</summary>
        private async Task<(Guid Id, string Email, string Phone)> UserAsync(string login, bool confirmedEmail = true, bool withPhone = true)
        {
            await api.SignUpAsync(login);
            var phone = "+998" + Interlocked.Increment(ref _phone);
            if (withPhone)
                await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '{phone}' where \"Login\" = '{login}'");
            if (confirmedEmail)
                await api.ScalarAsync<object>($"update \"Users\" set \"EmailConfirmedAt\" = now() where \"Login\" = '{login}'");
            return (await api.ScalarAsync<Guid>($"select \"Id\" from \"Users\" where \"Login\" = '{login}'"), $"{login}@tests.local", withPhone ? phone : "");
        }

        private async Task<int> NotifyAsync(Guid userId, NotificationType type, Dictionary<string, string> data, Guid? auctionId = null, bool once = false)
        {
            using var scope = api.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<INotificationService>().NotifyAsync(userId, type, data, auctionId, once);
        }

        private async Task<int> DispatchAsync(DateTime? now = null)
        {
            using var scope = api.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<INotificationDispatcher>().DispatchDueAsync(now ?? DateTime.UtcNow);
        }

        /// <summary>Notification emails to the address (the sign-up confirmation email is not one of them).</summary>
        private IEnumerable<FakeNotificationSinks.Email> MailTo(string address)
            => api.Sinks.EmailsTo(address).Where(e => e.Subject != Application.Notifications.EmailTemplates.EmailConfirmationSubject);

        private static Dictionary<string, string> Approved(string lot) => new() { ["lot"] = lot, ["number"] = "7", ["deposit"] = "5 000 000", ["start"] = "21.10.2026 14:00" };

        [Fact]
        public async Task A_reachable_user_gets_an_email_and_an_sms_with_the_filled_in_text()
        {
            var user = await UserAsync("notif_a1");

            Assert.Equal(2, await NotifyAsync(user.Id, NotificationType.APPLICATION_APPROVED, Approved("2026-000777")));
            await DispatchAsync();

            var email = Assert.Single(MailTo(user.Email).Where(e => e.Subject == "Ariza tasdiqlandi"));
            Assert.Contains("Lot 2026-000777: arizangiz tasdiqlandi", email.Html);
            Assert.Contains("Assalomu alaykum", email.Html);
            var sms = Assert.Single(api.Sinks.SmsTo(user.Phone));
            Assert.Equal("Lot 2026-000777: arizangiz tasdiqlandi. Ishtirokchi raqamingiz: 7. Zakalat: 5 000 000 so'm. Savdo 21.10.2026 14:00 da boshlanadi.", sms.Text);
            Assert.Equal(2L, await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"UserId\" = '{user.Id}' and \"Status\" = 'SENT'"));
        }

        [Fact]
        public async Task An_unconfirmed_email_a_missing_phone_and_email_only_messages_are_respected()
        {
            var unconfirmed = await UserAsync("notif_b1", confirmedEmail: false);
            var noPhone = await UserAsync("notif_b2", withPhone: false);

            // Unconfirmed email: SMS only. No phone: email only.
            Assert.Equal(1, await NotifyAsync(unconfirmed.Id, NotificationType.DEPOSIT_RECEIVED, new() { ["lot"] = "L1", ["amount"] = "5 000 000" }));
            Assert.Equal(1, await NotifyAsync(noPhone.Id, NotificationType.DEPOSIT_RECEIVED, new() { ["lot"] = "L1", ["amount"] = "5 000 000" }));
            // "Outbid" is email-only, even for someone with a phone and a confirmed email.
            var both = await UserAsync("notif_b3");
            Assert.Equal(1, await NotifyAsync(both.Id, NotificationType.OUTBID, new() { ["lot"] = "L1", ["price"] = "1" }));
            await DispatchAsync();

            Assert.Empty(MailTo(unconfirmed.Email));
            Assert.Single(api.Sinks.SmsTo(unconfirmed.Phone));
            Assert.Single(MailTo(noPhone.Email));
            Assert.Single(MailTo(both.Email));
            Assert.Empty(api.Sinks.SmsTo(both.Phone));
        }

        [Fact]
        public async Task A_reminder_marked_once_is_queued_only_once_per_user_and_auction()
        {
            var user = await UserAsync("notif_c1");
            var auction = Guid.NewGuid();
            var data = new Dictionary<string, string> { ["lot"] = "L2", ["start"] = "x" };

            Assert.Equal(2, await NotifyAsync(user.Id, NotificationType.AUCTION_STARTING_SOON, data, auction, once: true));
            Assert.Equal(0, await NotifyAsync(user.Id, NotificationType.AUCTION_STARTING_SOON, data, auction, once: true));
            // Another auction is another reminder.
            Assert.Equal(2, await NotifyAsync(user.Id, NotificationType.AUCTION_STARTING_SOON, data, Guid.NewGuid(), once: true));
        }

        [Fact]
        public async Task A_template_with_a_missing_or_extra_value_is_a_bug_and_throws()
        {
            var user = await UserAsync("notif_d1");

            await Assert.ThrowsAsync<InvalidOperationException>(() => NotifyAsync(user.Id, NotificationType.APPLICATION_REJECTED, new() { ["lot"] = "L3" }));
            await Assert.ThrowsAsync<InvalidOperationException>(() => NotifyAsync(user.Id, NotificationType.APPLICATION_REJECTED, new() { ["lot"] = "L3", ["reason"] = "x", ["extra"] = "y" }));
        }

        [Fact]
        public async Task A_failing_provider_is_retried_with_backoff_and_then_the_message_is_sent()
        {
            var user = await UserAsync("notif_e1", confirmedEmail: false);
            await NotifyAsync(user.Id, NotificationType.PAYMENT_RECEIVED, new() { ["lot"] = "L4" });
            var id = await api.ScalarAsync<Guid>($"select \"Id\" from \"Notifications\" where \"UserId\" = '{user.Id}'");
            var now = DateTime.UtcNow.AddSeconds(1);

            api.Sinks.SmsFails = true;
            try
            {
                await DispatchAsync(now);
                Assert.Equal(1, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));
                Assert.Equal("PENDING", await api.ScalarAsync<string>($"select \"Status\" from \"Notifications\" where \"Id\" = '{id}'"));
                Assert.Contains("unreachable", await api.ScalarAsync<string>($"select \"LastError\" from \"Notifications\" where \"Id\" = '{id}'") ?? "");

                // Not due again for a minute.
                await DispatchAsync(now.AddSeconds(30));
                Assert.Equal(1, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));
            }
            finally
            {
                api.Sinks.SmsFails = false;
            }

            await DispatchAsync(now.AddMinutes(2));
            Assert.Equal("SENT", await api.ScalarAsync<string>($"select \"Status\" from \"Notifications\" where \"Id\" = '{id}'"));
            Assert.Equal(2, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));
            Assert.Single(api.Sinks.SmsTo(user.Phone));
        }

        [Fact]
        public async Task A_message_that_keeps_failing_is_given_up_after_five_attempts()
        {
            var user = await UserAsync("notif_f1", confirmedEmail: false);
            await NotifyAsync(user.Id, NotificationType.PAYMENT_RECEIVED, new() { ["lot"] = "L5" });
            var id = await api.ScalarAsync<Guid>($"select \"Id\" from \"Notifications\" where \"UserId\" = '{user.Id}'");

            api.Sinks.SmsFails = true;
            try
            {
                var now = DateTime.UtcNow.AddSeconds(1);
                for (var i = 0; i < 5; i++)
                {
                    await DispatchAsync(now);
                    now = now.AddHours(3);   // past every backoff
                }
                Assert.Equal("FAILED", await api.ScalarAsync<string>($"select \"Status\" from \"Notifications\" where \"Id\" = '{id}'"));
                Assert.Equal(5, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));

                // Failed is final.
                await DispatchAsync(now);
                Assert.Equal(5, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));
            }
            finally
            {
                api.Sinks.SmsFails = false;
            }
        }

        [Fact]
        public async Task A_number_the_provider_refuses_is_not_retried()
        {
            var user = await UserAsync("notif_g1", confirmedEmail: false);
            await NotifyAsync(user.Id, NotificationType.PAYMENT_RECEIVED, new() { ["lot"] = "L6" });
            var id = await api.ScalarAsync<Guid>($"select \"Id\" from \"Notifications\" where \"UserId\" = '{user.Id}'");

            api.Sinks.SmsRefusedNumber = user.Phone;
            try
            {
                await DispatchAsync(DateTime.UtcNow.AddSeconds(1));
            }
            finally
            {
                api.Sinks.SmsRefusedNumber = null;
            }

            Assert.Equal("FAILED", await api.ScalarAsync<string>($"select \"Status\" from \"Notifications\" where \"Id\" = '{id}'"));
            Assert.Equal(1, await api.ScalarAsync<int>($"select \"Attempts\" from \"Notifications\" where \"Id\" = '{id}'"));
        }

        [Fact]
        public async Task Dispatchers_running_at_the_same_time_send_every_message_once()
        {
            var user = await UserAsync("notif_h1");
            for (var i = 0; i < 5; i++)
                await NotifyAsync(user.Id, NotificationType.DEPOSIT_RECEIVED, new() { ["lot"] = "L7-" + i, ["amount"] = "1" });
            var now = DateTime.UtcNow.AddSeconds(1);

            await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => DispatchAsync(now)));

            Assert.Equal(5, MailTo(user.Email).Count());
            Assert.Equal(5, api.Sinks.SmsTo(user.Phone).Count());
            Assert.Equal(10L, await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"UserId\" = '{user.Id}' and \"Status\" = 'SENT' and \"Attempts\" = 1"));
        }

        [Fact]
        public async Task The_delivery_log_is_for_admins_and_can_be_filtered()
        {
            var user = await UserAsync("notif_i1");
            await NotifyAsync(user.Id, NotificationType.PAYMENT_RECEIVED, new() { ["lot"] = "L8" });
            var admin = await api.AdminClientAsync();

            var list = await ApiFactory.JsonAsync(await admin.GetAsync($"api/notification/list?Filters%5Bdestination.equals%5D={user.Email}"));
            var item = Assert.Single(list["data"]!.AsArray())!;
            Assert.Equal("EMAIL", item["channel"]!.GetValue<string>());
            Assert.Equal("PAYMENT_RECEIVED", item["type"]!.GetValue<string>());
            Assert.Equal("PENDING", item["status"]!.GetValue<string>());

            await api.SignUpAsync("notif_plain");
            var plain = api.Authorized(await api.SignInAsync("notif_plain", "Strong@123"));
            Assert.Equal(HttpStatusCode.Forbidden, (await plain.GetAsync("api/notification/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/notification/list")).StatusCode);
        }
    }
}
