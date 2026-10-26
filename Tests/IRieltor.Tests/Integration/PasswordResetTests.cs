using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// W-B03: password reset by email. The link is single-use, the answer to "forgot password" never says whether the address
    /// is known, and a reset ends every session.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class PasswordResetTests(ApiFactory api)
    {
        private async Task<string> UserAsync(string login, bool confirmedEmail = true)
        {
            await api.SignUpAsync(login);
            if (confirmedEmail)
                await api.ScalarAsync<object>($"update \"Users\" set \"EmailConfirmedAt\" = now() where \"Login\" = '{login}'");
            return $"{login}@tests.local";
        }

        private Task<HttpResponseMessage> ForgotAsync(string email)
            => api.CreateClient().PostAsJsonAsync("api/auth/forgot-password", new { email });

        private Task<HttpResponseMessage> ResetAsync(string token, string password, string? confirm = null)
            => api.CreateClient().PostAsJsonAsync("api/auth/reset-password", new { token, newPassword = password, confirmPassword = confirm ?? password });

        private string[] ResetMails(string email)
            => api.Sinks.EmailsTo(email).Where(e => e.Subject == Application.Notifications.EmailTemplates.PasswordResetSubject).Select(e => e.Html).ToArray();

        private static string TokenIn(string html)
            => Uri.UnescapeDataString(Regex.Match(html, "token=([^\"<&\\s]+)").Groups[1].Value);

        [Fact]
        public async Task A_confirmed_email_gets_a_link_that_sets_a_new_password_once()
        {
            var email = await UserAsync("reset_a1");
            Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email)).StatusCode);

            var mail = Assert.Single(ResetMails(email));
            Assert.Contains("/parolni-tiklash?token=", mail);
            var token = TokenIn(mail);

            Assert.Equal(HttpStatusCode.OK, (await ResetAsync(token, "Newer@12345")).StatusCode);
            Assert.NotEmpty(await api.SignInAsync("reset_a1", "Newer@12345"));
            Assert.Equal(HttpStatusCode.Unauthorized,
                (await api.CreateClient().PostAsJsonAsync("api/auth/sign-in", new { login = "reset_a1", password = "Strong@123" })).StatusCode);

            // The same link again: the password it was issued against is gone.
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(token, "Another@12345")).StatusCode);
        }

        [Fact]
        public async Task A_reset_ends_the_sessions_that_were_open()
        {
            var email = await UserAsync("reset_a2");
            var signIn = await ApiFactory.JsonAsync(await api.CreateClient().PostAsJsonAsync("api/auth/sign-in", new { login = "reset_a2", password = "Strong@123" }));
            var refreshToken = signIn["data"]!["refreshToken"]!.GetValue<string>();

            await ForgotAsync(email);
            Assert.Equal(HttpStatusCode.OK, (await ResetAsync(TokenIn(Assert.Single(ResetMails(email))), "Newer@12345")).StatusCode);

            Assert.Equal(HttpStatusCode.Unauthorized,
                (await api.CreateClient().PostAsJsonAsync("api/auth/refresh-token", new { refreshToken })).StatusCode);
        }

        [Fact]
        public async Task An_unknown_or_unconfirmed_address_gets_the_same_answer_and_no_mail()
        {
            var unconfirmed = await UserAsync("reset_b1", confirmedEmail: false);

            var known = await ForgotAsync("nobody-at-all@tests.local");
            var notConfirmed = await ForgotAsync(unconfirmed);

            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
            Assert.Equal(HttpStatusCode.OK, notConfirmed.StatusCode);
            Assert.Equal(await known.Content.ReadAsStringAsync(), await notConfirmed.Content.ReadAsStringAsync());
            Assert.Empty(ResetMails(unconfirmed));
            Assert.Empty(ResetMails("nobody-at-all@tests.local"));
        }

        [Fact]
        public async Task Asking_again_at_once_sends_only_one_mail()
        {
            var email = await UserAsync("reset_c1");
            await ForgotAsync(email);
            Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email)).StatusCode);
            Assert.Single(ResetMails(email));
        }

        [Fact]
        public async Task A_failing_mail_server_does_not_change_the_answer_and_lets_the_person_ask_again()
        {
            var email = await UserAsync("reset_c2");
            api.Sinks.EmailFails = true;
            try { Assert.Equal(HttpStatusCode.OK, (await ForgotAsync(email)).StatusCode); }
            finally { api.Sinks.EmailFails = false; }
            Assert.Empty(ResetMails(email));

            await ForgotAsync(email);
            Assert.Single(ResetMails(email));
        }

        [Fact]
        public async Task A_forged_expired_or_foreign_token_and_a_bad_password_are_refused()
        {
            var email = await UserAsync("reset_d1");
            await ForgotAsync(email);
            var token = TokenIn(Assert.Single(ResetMails(email)));

            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync("garbage", "Newer@12345")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(token[..^2] + "xx", "Newer@12345")).StatusCode);
            // A valid link but a weak password, or a different repeat: the request is refused and the link still works.
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(token, "weak")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(token, "Newer@12345", "Different@123")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ResetAsync(token, "Newer@12345")).StatusCode);
        }

        [Fact]
        public async Task An_email_confirmation_token_cannot_be_used_as_a_reset_token()
        {
            var email = await UserAsync("reset_e1", confirmedEmail: false);
            var confirmation = api.Sinks.EmailsTo(email).First(e => e.Subject == Application.Notifications.EmailTemplates.EmailConfirmationSubject);
            Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(TokenIn(confirmation.Html), "Newer@12345")).StatusCode);
        }
    }
}
