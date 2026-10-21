using Microsoft.Extensions.DependencyInjection;
using Persistance.Services.Maintenance;
using System.Net;
using System.Net.Http.Json;

namespace IRieltor.Tests.Integration
{
    [Collection(ApiCollection.Name)]
    public class ApiTests(ApiFactory api)
    {
        [Fact]
        public async Task Health_endpoints_answer()
        {
            var client = api.CreateClient();
            Assert.Equal("Healthy", await client.GetStringAsync("health/live"));
            Assert.Equal("Healthy", await client.GetStringAsync("health/ready"));
        }

        [Fact]
        public async Task Sign_up_then_sign_in_and_read_own_profile()
        {
            await api.SignUpAsync("flow1");
            var client = api.Authorized(await api.SignInAsync("flow1", "Strong@123"));

            var me = await ApiFactory.JsonAsync(await client.GetAsync("api/auth/me"));
            Assert.Equal("flow1", me["data"]!["login"]!.GetValue<string>());
            Assert.Equal("flow1@tests.local", me["data"]!["email"]!.GetValue<string>());
        }

        [Fact]
        public async Task Wrong_password_is_401_in_the_common_shape()
        {
            var response = await api.CreateClient().PostAsJsonAsync("api/auth/sign-in", new { login = "nobody", password = "Wrong@123" });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var body = await ApiFactory.JsonAsync(response);
            Assert.False(body["success"]!.GetValue<bool>());
            Assert.Equal(401, body["statusCode"]!.GetValue<int>());   // a number, although enums are names elsewhere
            Assert.Equal("Invalid login or password.", body["message"]!.GetValue<string>());
        }

        [Fact]
        public async Task Validation_errors_list_the_fields()
        {
            var response = await api.CreateClient().PostAsJsonAsync("api/auth/sign-up",
                new { firstName = "A", lastName = "B", login = "v1", email = "not-an-email", password = "weak" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = await ApiFactory.JsonAsync(response);
            Assert.Equal(400, body["statusCode"]!.GetValue<int>());   // a number on this path too (MVC's serializer, which writes enums as names)
            var errors = body["errors"]!.AsArray().Select(e => e!.GetValue<string>()).ToList();
            Assert.Contains(errors, e => e.StartsWith("Email:"));
            Assert.Contains(errors, e => e.StartsWith("Password:"));
        }

        [Fact]
        public async Task Refresh_rotates_the_token_pair()
        {
            await api.SignUpAsync("refresh1");
            var signIn = await ApiFactory.JsonAsync(await api.CreateClient().PostAsJsonAsync("api/auth/sign-in", new { login = "refresh1", password = "Strong@123" }));
            var refreshToken = signIn["data"]!["refreshToken"]!.GetValue<string>();

            var response = await api.CreateClient().PostAsJsonAsync("api/auth/refresh-token", new { refreshToken });
            response.EnsureSuccessStatusCode();
            var data = (await ApiFactory.JsonAsync(response))["data"]!;

            Assert.NotEqual(refreshToken, data["refreshToken"]!.GetValue<string>());
            var me = await api.Authorized(data["accessToken"]!.GetValue<string>()).GetAsync("api/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        }

        [Fact]
        public async Task A_deleted_role_is_soft_deleted_and_gone_from_the_api()
        {
            var admin = await api.AdminClientAsync();
            var created = await ApiFactory.JsonAsync(await admin.PostAsync("api/userrole",
                new MultipartFormDataContent { { new StringContent("TEMP_ROLE"), "Name" } }));
            var id = created["data"]!["id"]!.GetValue<string>();

            var delete = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/userrole") { Content = JsonContent.Create(new { id }) });
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

            Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"api/userrole?Id={id}")).StatusCode);
            Assert.True(await api.ScalarAsync<bool>($"select \"IsDeleted\" from \"UserRoles\" where \"Id\" = '{id}'"));
        }

        [Fact]
        public async Task The_only_super_admin_cannot_delete_itself()
        {
            var admin = await api.AdminClientAsync();
            var id = await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'SUPER_ADMIN'");

            var response = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/user") { Content = JsonContent.Create(new { id }) });
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Empty_id_is_400_not_404()
        {
            var admin = await api.AdminClientAsync();
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.GetAsync($"api/user?Id={Guid.Empty}")).StatusCode);
        }

        [Fact]
        public async Task Request_id_is_echoed_or_created()
        {
            var client = api.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "health/live");
            request.Headers.Add("X-Request-ID", "abc-123");
            Assert.Equal("abc-123", (await client.SendAsync(request)).Headers.GetValues("X-Request-ID").Single());

            var bad = new HttpRequestMessage(HttpMethod.Get, "health/live");
            bad.Headers.Add("X-Request-ID", "has spaces and <tags>");
            Assert.NotEqual("has spaces and <tags>", (await client.SendAsync(bad)).Headers.GetValues("X-Request-ID").Single());
        }

        [Fact]
        public async Task Retention_anonymizes_old_deleted_users_and_removes_dead_tokens()
        {
            await api.SignUpAsync("retire1");
            await api.SignInAsync("retire1", "Strong@123");   // leaves a refresh token row
            var id = await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'retire1'");
            var admin = await api.AdminClientAsync();
            var delete = await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/user") { Content = JsonContent.Create(new { id }) });
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);

            // As if all of it happened long ago.
            await api.ScalarAsync<object>($"update \"Users\" set \"UpdatedAt\" = now() - interval '400 days' where \"Id\" = '{id}'");
            await api.ScalarAsync<object>($"update \"RefreshTokens\" set \"ExpiresAt\" = now() - interval '60 days' where \"UserId\" = '{id}'");

            var (tokens, users, _) = await api.Services.GetRequiredService<DataRetentionService>().RunOnceAsync(default);

            Assert.True(tokens >= 1);
            Assert.True(users >= 1);
            Assert.Equal(DataRetentionService.AnonymizedName, await api.ScalarAsync<string>($"select \"FullName\" from \"Users\" where \"Id\" = '{id}'"));
            Assert.Null(await api.ScalarAsync<string>($"select \"Email\" from \"Users\" where \"Id\" = '{id}'"));
            Assert.Null(await api.ScalarAsync<string>($"select \"Login\" from \"Users\" where \"Id\" = '{id}'"));
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"RefreshTokens\" where \"UserId\" = '{id}'"));
        }

        [Fact]
        public async Task Retention_gives_old_deleted_files_a_fresh_start_once_then_removes_them()
        {
            var retention = api.Services.GetRequiredService<DataRetentionService>();
            var folder = Path.Combine(api.FilesRoot, "deleted", "slide");
            Directory.CreateDirectory(folder);
            var file = Path.Combine(folder, "old.png");
            await File.WriteAllTextAsync(file, "x");
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(-400));   // moved there before the move time was recorded
            File.Delete(Path.Combine(api.FilesRoot, "deleted", DataRetentionService.StartedMarker));   // as on the first run after the update

            await retention.RunOnceAsync(default);
            Assert.True(File.Exists(file));                                   // first run: the period starts now

            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddDays(-91));
            var (_, _, removed) = await retention.RunOnceAsync(default);
            Assert.False(File.Exists(file));
            Assert.Equal(1, removed);
        }
    }
}
