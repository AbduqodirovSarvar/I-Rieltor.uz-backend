using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services.Maintenance;
using System.Net;
using System.Net.Http.Json;

namespace IRieltor.Tests.Integration
{
    /// <summary>P2-10: what the audit middleware records, what it leaves out, and who may read it.</summary>
    [Collection(ApiCollection.Name)]
    public class AuditLogTests(ApiFactory api)
    {
        /// <summary>
        /// The writer works in the background: waits until the request's row (found by its CorrelationId in the query) exists,
        /// then reads the column the query asks for.
        /// </summary>
        private async Task<T?> WaitForAsync<T>(string sql)
        {
            var correlationId = sql.Split("\"CorrelationId\" = '")[1].Split('\'')[0];
            for (var i = 0; i < 80; i++)
            {
                if (await api.ScalarAsync<long>($"select count(*) from \"AuditLogs\" where \"CorrelationId\" = '{correlationId}'") > 0)
                    return await api.ScalarAsync<T>(sql);
                await Task.Delay(100);
            }
            throw new TimeoutException($"No audit entry appeared for {correlationId}.");
        }

        private static HttpRequestMessage WithRequestId(HttpMethod method, string url, string requestId, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url) { Content = content };
            request.Headers.Add("X-Request-ID", requestId);
            return request;
        }

        [Fact]
        public async Task A_refused_sign_in_is_recorded_with_its_final_status_and_the_request_id_but_without_the_query()
        {
            var id = "audit-" + Guid.NewGuid().ToString("N")[..10];

            var response = await api.CreateClient().SendAsync(WithRequestId(HttpMethod.Post, "api/auth/sign-in?token=SECRET-TOKEN-123", id,
                JsonContent.Create(new { login = "nobody", password = "TopSecret@123" })));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

            Assert.Equal(401, await WaitForAsync<int>($"select \"StatusCode\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
            Assert.Equal("AuthController.SignIn", await api.ScalarAsync<string>($"select \"Action\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
            Assert.Equal("/api/auth/sign-in", await api.ScalarAsync<string>($"select \"Path\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
            Assert.Equal("POST", await api.ScalarAsync<string>($"select \"Method\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
            Assert.Null(await api.ScalarAsync<Guid?>($"select \"UserId\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
            // Neither the query string nor the body is kept.
            Assert.Equal(0L, await api.ScalarAsync<long>("select count(*) from \"AuditLogs\" where \"Path\" like '%SECRET%' or \"Action\" like '%SECRET%' or \"UserAgent\" like '%TopSecret%'"));
        }

        [Fact]
        public async Task A_signed_in_change_carries_the_user_and_an_error_status_is_the_one_the_client_got()
        {
            var admin = await api.AdminClientAsync();
            var adminId = await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'SUPER_ADMIN'");
            var name = "AUDIT_" + Guid.NewGuid().ToString("N")[..8];
            var created = "audit-ok-" + Guid.NewGuid().ToString("N")[..8];
            var duplicate = "audit-dup-" + Guid.NewGuid().ToString("N")[..8];

            Assert.Equal(HttpStatusCode.OK, (await admin.SendAsync(WithRequestId(HttpMethod.Post, "api/userrole", created, new MultipartFormDataContent { { new StringContent(name), "Name" } }))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(WithRequestId(HttpMethod.Post, "api/userrole", duplicate, new MultipartFormDataContent { { new StringContent(name), "Name" } }))).StatusCode);

            Assert.Equal(200, await WaitForAsync<int>($"select \"StatusCode\" from \"AuditLogs\" where \"CorrelationId\" = '{created}'"));
            Assert.Equal(adminId, await api.ScalarAsync<Guid>($"select \"UserId\" from \"AuditLogs\" where \"CorrelationId\" = '{created}'"));
            Assert.Equal("UserRoleController.Post", await api.ScalarAsync<string>($"select \"Action\" from \"AuditLogs\" where \"CorrelationId\" = '{created}'"));
            // The handler turned an exception into 409; that is what is recorded, not a 500.
            Assert.Equal(409, await WaitForAsync<int>($"select \"StatusCode\" from \"AuditLogs\" where \"CorrelationId\" = '{duplicate}'"));
            var occurred = await api.ScalarAsync<DateTime>($"select \"OccurredAt\" from \"AuditLogs\" where \"CorrelationId\" = '{created}'");
            Assert.InRange(occurred, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddSeconds(1));
        }

        [Fact]
        public async Task Plain_reads_are_not_recorded()
        {
            var admin = await api.AdminClientAsync();
            var id = "audit-get-" + Guid.NewGuid().ToString("N")[..8];
            var before = await api.ScalarAsync<long>("select count(*) from \"AuditLogs\" where \"Path\" = '/api/userrole/list'");

            await admin.SendAsync(WithRequestId(HttpMethod.Get, "api/userrole/list", id));
            await api.CreateClient().GetAsync("api/slide/public");
            // A marker that is written after the reads, so waiting for it means the earlier entries would have been written too.
            var marker = "audit-marker-" + Guid.NewGuid().ToString("N")[..8];
            await api.CreateClient().SendAsync(WithRequestId(HttpMethod.Post, "api/auth/sign-in", marker, JsonContent.Create(new { login = "x", password = "y" })));
            await WaitForAsync<int>($"select \"StatusCode\" from \"AuditLogs\" where \"CorrelationId\" = '{marker}'");

            Assert.Equal(before, await api.ScalarAsync<long>("select count(*) from \"AuditLogs\" where \"Path\" = '/api/userrole/list'"));
            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
        }

        [Fact]
        public async Task The_log_is_for_admins_and_can_be_filtered()
        {
            var id = "audit-list-" + Guid.NewGuid().ToString("N")[..8];
            await api.CreateClient().SendAsync(WithRequestId(HttpMethod.Post, "api/auth/sign-in", id, JsonContent.Create(new { login = "x", password = "y" })));
            await WaitForAsync<int>($"select \"StatusCode\" from \"AuditLogs\" where \"CorrelationId\" = '{id}'");
            var admin = await api.AdminClientAsync();

            var list = await ApiFactory.JsonAsync(await admin.GetAsync($"api/auditlog/list?Filters%5BcorrelationId.equals%5D={id}"));
            var item = Assert.Single(list["data"]!.AsArray())!;
            Assert.Equal(401, item["statusCode"]!.GetValue<int>());
            Assert.Equal("/api/auth/sign-in", item["path"]!.GetValue<string>());

            await api.SignUpAsync("audit_plain");
            Assert.Equal(HttpStatusCode.Forbidden, (await api.Authorized(await api.SignInAsync("audit_plain", "Strong@123")).GetAsync("api/auditlog/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/auditlog/list")).StatusCode);
        }

        [Fact]
        public async Task Old_entries_are_deleted_by_the_retention_job()
        {
            var id = "audit-old-" + Guid.NewGuid().ToString("N")[..8];
            await api.ScalarAsync<object>($"insert into \"AuditLogs\" (\"Id\", \"CreatedAt\", \"IsDeleted\", \"OccurredAt\", \"Action\", \"Method\", \"Path\", \"StatusCode\", \"CorrelationId\", \"DurationMs\") " +
                                          $"values (gen_random_uuid(), now() - interval '400 days', false, now() - interval '400 days', 'x', 'POST', '/api/x', 200, '{id}', 1)");
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));

            await api.Services.GetRequiredService<DataRetentionService>().RunOnceAsync(default);

            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"AuditLogs\" where \"CorrelationId\" = '{id}'"));
        }
    }
}
