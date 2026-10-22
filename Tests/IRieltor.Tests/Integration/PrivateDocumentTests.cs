using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Infrastructure.Services.Files;
using Infrastructure.Services.Maintenance;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace IRieltor.Tests.Integration
{
    /// <summary>P2-09: personal documents are stored where no URL leads and given back only after the access check.</summary>
    [Collection(ApiCollection.Name)]
    public class PrivateDocumentTests(ApiFactory api)
    {
        private static byte[] Pdf(string text = "hello") => Encoding.ASCII.GetBytes("%PDF-1.4\n% " + text + "\n");
        private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        private async Task<HttpClient> UserAsync(string login)
        {
            await api.SignUpAsync(login);
            return api.Authorized(await api.SignInAsync(login, "Strong@123"));
        }

        private static Task<HttpResponseMessage> UploadAsync(HttpClient client, byte[] bytes, string fileName, string purpose = "IDENTITY", string? description = null)
        {
            var content = new ByteArrayContent(bytes);
            var form = new MultipartFormDataContent { { content, "File", fileName }, { new StringContent(purpose), "Purpose" } };
            if (description != null)
                form.Add(new StringContent(description), "Description");
            return client.PostAsync("api/document", form);
        }

        private static async Task<JsonObject> DocumentAsync(HttpResponseMessage response)
        {
            var body = await ApiFactory.JsonAsync(response);
            Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
            return body["data"]!.AsObject();
        }

        private string StoredName(string id) => api.ScalarAsync<string>($"select \"StoredName\" from \"PrivateDocuments\" where \"Id\" = '{id}'").Result!;

        [Fact]
        public async Task The_owner_uploads_and_downloads_a_document_that_has_no_public_address()
        {
            var owner = await UserAsync("doc_owner1");
            var bytes = Pdf("passport of owner1");

            var document = await DocumentAsync(await UploadAsync(owner, bytes, "pasport nusxasi.pdf", "IDENTITY", " old tomoni "));
            var id = document["id"]!.GetValue<string>();
            Assert.Equal("pasport nusxasi.pdf", document["originalName"]!.GetValue<string>());
            Assert.Equal("application/pdf", document["contentType"]!.GetValue<string>());
            Assert.Equal(bytes.Length, document["size"]!.GetValue<long>());
            Assert.Equal("old tomoni", document["description"]!.GetValue<string>());
            Assert.Null(document["storedName"]);   // the disk name is never returned

            var stored = StoredName(id);
            Assert.True(File.Exists(Path.Combine(api.FilesRoot, "private", stored)));
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"private/{stored}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"uploads/private/{stored}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"uploads/../private/{stored}")).StatusCode);

            var download = await owner.GetAsync($"api/document/download?id={id}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal(bytes, await download.Content.ReadAsByteArrayAsync());
            Assert.Equal("application/pdf", download.Content.Headers.ContentType!.MediaType);
            Assert.Equal("attachment", download.Content.Headers.ContentDisposition!.DispositionType);
            Assert.Contains("no-store", download.Headers.CacheControl!.ToString());
            Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Contains("sandbox", download.Headers.GetValues("Content-Security-Policy").Single());

            var mine = (await ApiFactory.JsonAsync(await owner.GetAsync("api/document/my/list")))["data"]!.AsArray();
            Assert.Contains(mine, d => d!["id"]!.GetValue<string>() == id);
        }

        [Fact]
        public async Task Other_users_cannot_download_list_or_delete_it_and_cannot_tell_it_exists()
        {
            var owner = await UserAsync("doc_owner2");
            var stranger = await UserAsync("doc_stranger2");
            var id = (await DocumentAsync(await UploadAsync(owner, Pdf("secret"), "secret.pdf")))["id"]!.GetValue<string>();

            // The same answer as for an id that does not exist.
            var existing = await stranger.GetAsync($"api/document/download?id={id}");
            var missing = await stranger.GetAsync($"api/document/download?id={Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, existing.StatusCode);
            Assert.Equal(await missing.Content.ReadAsStringAsync(), await existing.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/document") { Content = JsonContent.Create(new { id }) })).StatusCode);

            Assert.DoesNotContain((await ApiFactory.JsonAsync(await stranger.GetAsync("api/document/my/list")))["data"]!.AsArray(), d => d!["id"]!.GetValue<string>() == id);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync("api/document/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync($"api/document/download?id={id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsync("api/document", new MultipartFormDataContent())).StatusCode);

            // Staff with READ_PRIVATEDOCUMENT see everything.
            var admin = await api.AdminClientAsync();
            Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"api/document/download?id={id}")).StatusCode);
            var all = (await ApiFactory.JsonAsync(await admin.GetAsync($"api/document/list?Filters%5BownerId.equals%5D={await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'doc_owner2'")}")))["data"]!.AsArray();
            Assert.Single(all);
        }

        [Fact]
        public async Task Only_documents_that_are_what_they_claim_to_be_are_accepted()
        {
            var user = await UserAsync("doc_user3");

            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, Pdf(), "virus.exe")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, Pdf(), "page.html")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, Pdf(), "disguised.png")).StatusCode);   // pdf content, png name
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, [], "empty.pdf")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, Png.Concat(new byte[11 * 1024 * 1024]).ToArray(), "huge.png")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await UploadAsync(user, Png, "photo.PNG")).StatusCode);
            // Video and spreadsheets are fine as public files, not as personal documents.
            Assert.Equal(HttpStatusCode.BadRequest, (await UploadAsync(user, [0, 0, 0, 0x18, (byte)'f', (byte)'t', (byte)'y', (byte)'p'], "clip.mp4")).StatusCode);

            // References must point at something real.
            var form = new MultipartFormDataContent { { new ByteArrayContent(Pdf()), "File", "x.pdf" }, { new StringContent(Guid.NewGuid().ToString()), "AuctionId" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await user.PostAsync("api/document", form)).StatusCode);
            form = new MultipartFormDataContent { { new ByteArrayContent(Pdf()), "File", "x.pdf" }, { new StringContent(Guid.NewGuid().ToString()), "PaymentId" } };
            Assert.Equal(HttpStatusCode.BadRequest, (await user.PostAsync("api/document", form)).StatusCode);

            // A client-chosen name with a path in it is only a label; the file lands under a random name.
            var sneaky = await DocumentAsync(await UploadAsync(user, Pdf(), "..\\..\\evil/../name.pdf"));
            Assert.Equal("name.pdf", sneaky["originalName"]!.GetValue<string>());
            Assert.DoesNotContain("..", StoredName(sneaky["id"]!.GetValue<string>()));
        }

        [Fact]
        public async Task A_deleted_document_is_gone_for_good_and_its_file_waits_in_deleted()
        {
            var owner = await UserAsync("doc_owner4");
            var id = (await DocumentAsync(await UploadAsync(owner, Pdf("to delete"), "d.pdf")))["id"]!.GetValue<string>();
            var stored = StoredName(id);

            Assert.Equal(HttpStatusCode.OK, (await owner.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/document") { Content = JsonContent.Create(new { id }) })).StatusCode);

            Assert.False(File.Exists(Path.Combine(api.FilesRoot, "private", stored)));
            Assert.True(File.Exists(Path.Combine(api.FilesRoot, "deleted", "private", stored)));
            Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"api/document/download?id={id}")).StatusCode);
        }

        [Fact]
        public async Task A_download_is_recorded_in_the_audit_log()
        {
            var owner = await UserAsync("doc_owner5");
            var id = (await DocumentAsync(await UploadAsync(owner, Pdf("audit me"), "a.pdf")))["id"]!.GetValue<string>();
            var request = new HttpRequestMessage(HttpMethod.Get, $"api/document/download?id={id}");
            var requestId = "doc-dl-" + Guid.NewGuid().ToString("N")[..8];
            request.Headers.Add("X-Request-ID", requestId);

            await owner.SendAsync(request);

            for (var i = 0; i < 80 && await api.ScalarAsync<long>($"select count(*) from \"AuditLogs\" where \"CorrelationId\" = '{requestId}'") == 0; i++)
                await Task.Delay(100);
            Assert.Equal("DocumentController.Download", await api.ScalarAsync<string>($"select \"Action\" from \"AuditLogs\" where \"CorrelationId\" = '{requestId}'"));
            // The query string with the id is not part of the stored path.
            Assert.Equal("/api/document/download", await api.ScalarAsync<string>($"select \"Path\" from \"AuditLogs\" where \"CorrelationId\" = '{requestId}'"));
        }

        [Fact]
        public async Task The_documents_of_a_user_removed_long_ago_are_deleted_with_them()
        {
            var owner = await UserAsync("doc_gone6");
            var id = (await DocumentAsync(await UploadAsync(owner, Pdf("old"), "o.pdf")))["id"]!.GetValue<string>();
            var stored = StoredName(id);
            var userId = await api.ScalarAsync<Guid>("select \"Id\" from \"Users\" where \"Login\" = 'doc_gone6'");
            var admin = await api.AdminClientAsync();
            Assert.Equal(HttpStatusCode.OK, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/user") { Content = JsonContent.Create(new { id = userId }) })).StatusCode);
            await api.ScalarAsync<object>($"update \"Users\" set \"UpdatedAt\" = now() - interval '400 days' where \"Id\" = '{userId}'");

            await api.Services.GetRequiredService<DataRetentionService>().RunOnceAsync(default);

            Assert.Equal(0L, await api.ScalarAsync<long>($"select count(*) from \"PrivateDocuments\" where \"Id\" = '{id}'"));
            Assert.False(File.Exists(Path.Combine(api.FilesRoot, "private", stored)));
            Assert.False(File.Exists(Path.Combine(api.FilesRoot, "deleted", "private", stored)));
        }

        [Fact]
        public async Task A_private_file_without_a_record_is_moved_away_by_the_orphan_cleanup()
        {
            var folder = Path.Combine(api.FilesRoot, "private");
            Directory.CreateDirectory(folder);
            var orphan = Path.Combine(folder, Guid.NewGuid() + ".pdf");
            await File.WriteAllBytesAsync(orphan, Pdf("orphan"));
            File.SetLastWriteTimeUtc(orphan, DateTime.UtcNow.AddDays(-3));
            var young = Path.Combine(folder, Guid.NewGuid() + ".pdf");
            await File.WriteAllBytesAsync(young, Pdf("being uploaded"));

            var cleanup = api.Services.GetServices<IHostedService>().OfType<OrphanFileCleanupService>().Single();
            await cleanup.RunOnceAsync(default);

            Assert.False(File.Exists(orphan));
            Assert.True(File.Exists(Path.Combine(api.FilesRoot, "deleted", "orphans", "private", Path.GetFileName(orphan))));
            Assert.True(File.Exists(young));   // an upload in progress is left alone
        }
    }
}
