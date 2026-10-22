using System.Net;
using System.Net.Http.Json;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-13 (a key linked to a login/password account) and P2-12 (E-IMZO Mobile / ID-card sessions).
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class EimzoLinkAndMobileTests(ApiFactory api)
    {
        private async Task<HttpClient> SignedUpUserAsync(string login)
        {
            await api.SignUpAsync(login);
            return api.Authorized(await api.SignInAsync(login, "Strong@123"));
        }

        [Fact]
        public async Task A_signed_challenge_links_the_key_and_makes_the_account_verified()
        {
            var user = await SignedUpUserAsync("link_a1");
            var pinfl = ApiFactory.NewPinfl();

            // A password account starts unverified.
            Assert.Equal("NONE", (await ApiFactory.JsonAsync(await user.GetAsync("api/auth/me")))["data"]!["type"]!.GetValue<string>());

            var linked = await user.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) });
            Assert.Equal(HttpStatusCode.OK, linked.StatusCode);
            var profile = (await ApiFactory.JsonAsync(linked))["data"]!;
            Assert.Equal("INDIVIDUAL", profile["type"]!.GetValue<string>());
            Assert.Equal(pinfl, profile["innpinfl"]!.GetValue<string>());

            // The same key again changes nothing; the account can now also sign in with it.
            Assert.Equal(HttpStatusCode.OK, (await user.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) })).StatusCode);
            var viaKey = await api.CreateClient().PostAsJsonAsync("api/auth/sign-in-via-eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) });
            Assert.Equal(HttpStatusCode.OK, viaKey.StatusCode);
        }

        [Fact]
        public async Task A_legal_entity_key_makes_a_legal_account_with_the_organisation_name()
        {
            var user = await SignedUpUserAsync("link_legal");
            var response = await user.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(ApiFactory.NewPinfl(), legalInn: "301234" + Random.Shared.Next(100, 999)) });

            var profile = (await ApiFactory.JsonAsync(response))["data"]!;
            Assert.Equal("LEGAL", profile["type"]!.GetValue<string>());
            Assert.Equal("ORG MCHJ", profile["legalName"]!.GetValue<string>());
        }

        [Fact]
        public async Task One_key_per_account_and_one_account_per_key()
        {
            var first = await SignedUpUserAsync("link_b1");
            var second = await SignedUpUserAsync("link_b2");
            var pinfl = ApiFactory.NewPinfl();
            await first.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) });

            // The key is taken; the first account cannot take a second key.
            Assert.Equal(HttpStatusCode.Conflict, (await second.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(pinfl) })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await first.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(ApiFactory.NewPinfl()) })).StatusCode);
        }

        [Fact]
        public async Task A_signed_challenge_works_once_and_linking_needs_sign_in()
        {
            var user = await SignedUpUserAsync("link_c1");
            var signed = await api.SignedChallengeAsync(ApiFactory.NewPinfl());

            Assert.Equal(HttpStatusCode.OK, (await user.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = signed })).StatusCode);
            var replay = await api.CreateClient().PostAsJsonAsync("api/auth/sign-in-via-eimzo", new { signedData = signed });
            Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);   // the challenge is single-use

            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsJsonAsync("api/auth/me/eimzo", new { signedData = signed })).StatusCode);
        }

        [Fact]
        public async Task A_phone_session_signs_up_signs_in_and_resets_the_password()
        {
            var pinfl = ApiFactory.NewPinfl();
            var client = api.CreateClient();

            async Task<string> FinishedSessionAsync()
            {
                var started = (await ApiFactory.JsonAsync(await client.PostAsync("api/auth/eimzo/mobile/auth/start", null)))["data"]!;
                var documentId = started["documentId"]!.GetValue<string>();
                Assert.Equal("TESTSITE", started["siteId"]!.GetValue<string>());
                Assert.False(string.IsNullOrEmpty(started["challenge"]!.GetValue<string>()));

                // Waiting for the phone.
                var waiting = (await ApiFactory.JsonAsync(await client.GetAsync($"api/auth/eimzo/mobile/status?DocumentId={documentId}")))["data"]!;
                Assert.True(waiting["isPending"]!.GetValue<bool>());
                Assert.False(waiting["isReady"]!.GetValue<bool>());

                api.Eimzo.Upload(documentId, pinfl);
                var ready = (await ApiFactory.JsonAsync(await client.GetAsync($"api/auth/eimzo/mobile/status?DocumentId={documentId}")))["data"]!;
                Assert.True(ready["isReady"]!.GetValue<bool>());
                return documentId;
            }

            // Sign-up, then sign-in, each with its own phone session.
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("api/auth/sign-up-via-eimzo-mobile", new { documentId = await FinishedSessionAsync() })).StatusCode);
            var signIn = await client.PostAsJsonAsync("api/auth/sign-in-via-eimzo-mobile", new { documentId = await FinishedSessionAsync() });
            Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
            Assert.False(string.IsNullOrEmpty((await ApiFactory.JsonAsync(signIn))["data"]!["accessToken"]!.GetValue<string>()));

            var reset = await client.PostAsJsonAsync("api/auth/reset-password-via-eimzo-mobile",
                new { documentId = await FinishedSessionAsync(), newPassword = "NewStrong@456", confirmPassword = "NewStrong@456" });
            Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
            var login = await api.ScalarAsync<string>($"select \"Id\"::text from \"Users\" where \"INNPINFL\" = '{pinfl}'");
            Assert.NotNull(login);
            Assert.NotEmpty(await api.ScalarAsync<string>($"select \"PasswordHash\" from \"Users\" where \"INNPINFL\" = '{pinfl}'") ?? "");
        }

        [Fact]
        public async Task A_phone_session_that_is_unknown_or_unsigned_is_refused()
        {
            var client = api.CreateClient();
            var started = (await ApiFactory.JsonAsync(await client.PostAsync("api/auth/eimzo/mobile/auth/start", null)))["data"]!;
            var documentId = started["documentId"]!.GetValue<string>();

            // The phone has not signed yet.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("api/auth/sign-in-via-eimzo-mobile", new { documentId })).StatusCode);
            // Not a session at all.
            var unknown = (await ApiFactory.JsonAsync(await client.GetAsync("api/auth/eimzo/mobile/status?DocumentId=nope")))["data"]!;
            Assert.Equal(-2, unknown["status"]!.GetValue<int>());
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("api/auth/sign-in-via-eimzo-mobile", new { documentId = "nope" })).StatusCode);
            // Signing sessions need a signed-in user.
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("api/auth/eimzo/mobile/sign/start", null)).StatusCode);
        }

        [Fact]
        public async Task A_phone_session_links_the_key_to_the_signed_in_account()
        {
            var user = await SignedUpUserAsync("link_mobile");
            var pinfl = ApiFactory.NewPinfl();
            var documentId = (await ApiFactory.JsonAsync(await api.CreateClient().PostAsync("api/auth/eimzo/mobile/auth/start", null)))["data"]!["documentId"]!.GetValue<string>();
            api.Eimzo.Upload(documentId, pinfl);

            var response = await user.PostAsJsonAsync("api/auth/me/eimzo-mobile", new { documentId });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(pinfl, (await ApiFactory.JsonAsync(response))["data"]!["innpinfl"]!.GetValue<string>());
        }
    }
}
