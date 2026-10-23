using System.Net;
using System.Net.Http.Json;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// W-B07: a signed-in user edits their own contact details through PUT api/auth/me/profile. A plain user has no
    /// UPDATE_USER permission, so PUT api/user is not an option for them.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class ProfileTests(ApiFactory api)
    {
        private async Task<HttpClient> UserAsync(string login)
        {
            await api.SignUpAsync(login);
            return api.Authorized(await api.SignInAsync(login, "Strong@123"));
        }

        private static async Task<System.Text.Json.Nodes.JsonNode> MeAsync(HttpClient client)
            => (await ApiFactory.JsonAsync(await client.GetAsync("api/auth/me")))["data"]!;

        [Fact]
        public async Task A_user_edits_name_phones_and_address_and_the_profile_shows_them()
        {
            var user = await UserAsync("prof_a1");

            var response = await user.PutAsJsonAsync("api/auth/me/profile",
                new { fullName = "  Valiyev   Ali  Karimovich ", phone = "+998901112233", secondaryPhone = "+998902223344", address = " Toshkent, Amir Temur 1 " });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var me = await MeAsync(user);
            Assert.Equal("Valiyev Ali Karimovich", me["fullName"]!.GetValue<string>());
            Assert.Equal("+998901112233", me["phone"]!.GetValue<string>());
            Assert.Equal("+998902223344", me["secondaryPhone"]!.GetValue<string>());
            Assert.Equal("Toshkent, Amir Temur 1", me["address"]!.GetValue<string>());
        }

        [Fact]
        public async Task A_value_left_out_stays_and_an_empty_text_clears_a_phone_or_the_address()
        {
            var user = await UserAsync("prof_a2");
            await user.PutAsJsonAsync("api/auth/me/profile", new { phone = "+998903334455", address = "Manzil" });

            // Only the address is sent: the phone stays.
            await user.PutAsJsonAsync("api/auth/me/profile", new { address = "Yangi manzil" });
            var me = await MeAsync(user);
            Assert.Equal("+998903334455", me["phone"]!.GetValue<string>());
            Assert.Equal("Yangi manzil", me["address"]!.GetValue<string>());

            await user.PutAsJsonAsync("api/auth/me/profile", new { phone = "", address = "" });
            me = await MeAsync(user);
            Assert.Null(me["phone"]);
            Assert.Null(me["address"]);
            Assert.Equal("User Test", me["fullName"]!.GetValue<string>());
        }

        [Fact]
        public async Task Bad_input_is_400_and_a_taken_phone_is_409()
        {
            var first = await UserAsync("prof_b1");
            var second = await UserAsync("prof_b2");
            Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("api/auth/me/profile", new { phone = "+998904445566" })).StatusCode);

            Assert.Equal(HttpStatusCode.BadRequest, (await second.PutAsJsonAsync("api/auth/me/profile", new { fullName = "   " })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await second.PutAsJsonAsync("api/auth/me/profile", new { phone = "998901112233" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await second.PutAsJsonAsync("api/auth/me/profile", new { phone = "+998904445566" })).StatusCode);
            // The same user keeping their own phone is not a conflict.
            Assert.Equal(HttpStatusCode.OK, (await first.PutAsJsonAsync("api/auth/me/profile", new { phone = "+998904445566" })).StatusCode);
        }

        [Fact]
        public async Task An_account_verified_with_a_key_keeps_the_name_of_the_key_but_can_change_the_rest()
        {
            var user = await UserAsync("prof_c1");
            await user.PostAsJsonAsync("api/auth/me/eimzo", new { signedData = await api.SignedChallengeAsync(ApiFactory.NewPinfl()) });
            var name = (await MeAsync(user))["fullName"]!.GetValue<string>();

            Assert.Equal(HttpStatusCode.BadRequest, (await user.PutAsJsonAsync("api/auth/me/profile", new { fullName = name + " X" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await user.PutAsJsonAsync("api/auth/me/profile", new { fullName = name, address = "Samarqand" })).StatusCode);
            var me = await MeAsync(user);
            Assert.Equal(name, me["fullName"]!.GetValue<string>());
            Assert.Equal("Samarqand", me["address"]!.GetValue<string>());
            Assert.Equal("INDIVIDUAL", me["type"]!.GetValue<string>());
        }

        [Fact]
        public async Task Without_signing_in_it_is_401_and_the_credentials_cannot_be_changed_through_it()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PutAsJsonAsync("api/auth/me/profile", new { address = "x" })).StatusCode);

            var user = await UserAsync("prof_d1");
            // Unknown members (login, e-mail, password, type) are ignored, not applied.
            await user.PutAsJsonAsync("api/auth/me/profile", new { login = "hacker", email = "x@y.z", password = "Hacked@123", type = "LEGAL", address = "ok" });
            var me = await MeAsync(user);
            Assert.Equal("prof_d1", me["login"]!.GetValue<string>());
            Assert.Equal("prof_d1@tests.local", me["email"]!.GetValue<string>());
            Assert.Equal("NONE", me["type"]!.GetValue<string>());
            Assert.NotNull(await api.SignInAsync("prof_d1", "Strong@123"));
        }
    }
}
