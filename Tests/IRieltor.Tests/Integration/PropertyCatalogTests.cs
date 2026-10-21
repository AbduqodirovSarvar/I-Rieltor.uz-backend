using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Persistance.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-02: categories and properties for admins, and the public catalog.
    /// Every test works in its own region (a fresh name), so tests sharing the database do not see each other's rows.
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class PropertyCatalogTests(ApiFactory api)
    {
        // A 1x1 PNG and the start of a PDF: enough for the content checks of the upload.
        private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        private static readonly byte[] Pdf = "%PDF-1.4\n%test\n"u8.ToArray();

        private static MultipartFormDataContent Form(params (string Name, string Value)[] fields)
        {
            var form = new MultipartFormDataContent();
            foreach (var (name, value) in fields)
                form.Add(new StringContent(value), name);
            return form;
        }

        private static async Task<JsonObject> OkData(HttpResponseMessage response)
        {
            var body = await ApiFactory.JsonAsync(response);
            Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
            return body;
        }

        private async Task<string> CreateCategoryAsync(HttpClient admin, string name, string? parentId = null)
        {
            var fields = new List<(string, string)> { ("Name", name) };
            if (parentId != null) fields.Add(("ParentCategoryId", parentId));
            return (await OkData(await admin.PostAsync("api/propertycategory", Form([.. fields]))))["data"]!["id"]!.GetValue<string>();
        }

        private static (string, string)[] PropertyFields(string categoryId, string region, string status = "PUBLISHED", string area = "50", string rooms = "2", string address = "Markaz 1")
            => [("Title", "Obyekt " + region), ("CategoryId", categoryId), ("Region", region), ("Address", address), ("AreaSqm", area), ("Rooms", rooms), ("Status", status)];

        private async Task<string> CreatePropertyAsync(HttpClient admin, params (string, string)[] fields)
            => (await OkData(await admin.PostAsync("api/property", Form(fields))))["data"]!["id"]!.GetValue<string>();

        private static HttpContent File(byte[] bytes, string name, string contentType)
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            return content;
        }

        private static Task<HttpResponseMessage> AddMediaAsync(HttpClient admin, string propertyId, byte[] bytes, string fileName, string contentType, string kind, bool cover = false)
        {
            var form = Form(("PropertyId", propertyId), ("Kind", kind), ("IsCover", cover ? "true" : "false"), ("Title", fileName));
            form.Add(File(bytes, fileName, contentType), "File", fileName);
            return admin.PostAsync("api/property/media", form);
        }

        private Task<HttpResponseMessage> PublicListAsync(string query) => api.CreateClient().GetAsync("api/property/public/list?" + query);

        private static List<string> Titles(JsonObject body) => body["data"]!.AsArray().Select(x => x!["title"]!.GetValue<string>()).ToList();

        private static string Region() => "Viloyat-" + Guid.NewGuid().ToString("N")[..8];

        [Fact]
        public async Task Categories_form_a_tree_with_unique_names_and_no_cycles()
        {
            var admin = await api.AdminClientAsync();
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var root = await CreateCategoryAsync(admin, "Turar joy " + suffix);
            var child = await CreateCategoryAsync(admin, "Kvartira " + suffix, root);

            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync("api/propertycategory", Form(("Name", "TURAR JOY " + suffix)))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/propertycategory", Form(("Name", "x" + suffix), ("ParentCategoryId", Guid.NewGuid().ToString())))).StatusCode);
            // The root under its own child would be a cycle.
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsync("api/propertycategory", Form(("Id", root), ("Name", "Turar joy " + suffix), ("ParentCategoryId", child)))).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/propertycategory") { Content = JsonContent.Create(new { id = root }) })).StatusCode);

            var list = await OkData(await api.CreateClient().GetAsync("api/propertycategory/public"));
            var mine = list["data"]!.AsArray().Where(c => c!["name"]!.GetValue<string>().EndsWith(suffix)).ToList();
            Assert.Equal(2, mine.Count);
            Assert.Equal(root, mine.Single(c => c!["id"]!.GetValue<string>() == child)!["parentCategoryId"]!.GetValue<string>());
        }

        [Fact]
        public async Task Only_published_properties_reach_the_catalog_and_auction_statuses_are_not_set_by_hand()
        {
            var admin = await api.AdminClientAsync();
            var category = await CreateCategoryAsync(admin, "Uy " + Guid.NewGuid().ToString("N")[..6]);
            var region = Region();
            var id = await CreatePropertyAsync(admin, PropertyFields(category, region, status: "DRAFT"));

            Assert.Empty(Titles(await OkData(await PublicListAsync($"Region={region}"))));
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"api/property/public?Id={id}")).StatusCode);

            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PutAsync("api/property", Form([("Id", id), .. PropertyFields(category, region, status: "IN_AUCTION")]))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("api/property", Form(PropertyFields(category, Region(), status: "SOLD")))).StatusCode);

            await OkData(await admin.PutAsync("api/property", Form([("Id", id), .. PropertyFields(category, region, status: "PUBLISHED")])));
            Assert.Single(Titles(await OkData(await PublicListAsync($"Region={region.ToUpperInvariant()}"))));

            // Archived: gone from the catalog again.
            await OkData(await admin.PutAsync("api/property", Form([("Id", id), .. PropertyFields(category, region, status: "ARCHIVED")])));
            Assert.Empty(Titles(await OkData(await PublicListAsync($"Region={region}"))));
        }

        [Fact]
        public async Task Media_kinds_are_checked_and_one_image_is_the_cover()
        {
            var admin = await api.AdminClientAsync();
            var category = await CreateCategoryAsync(admin, "Ofis " + Guid.NewGuid().ToString("N")[..6]);
            var region = Region();
            var id = await CreatePropertyAsync(admin, PropertyFields(category, region));
            var filesBefore = await api.ScalarAsync<long>("select count(*) from \"UploadedFiles\" where not \"IsDeleted\"");

            var first = (await OkData(await AddMediaAsync(admin, id, Png, "a.png", "image/png", "IMAGE", cover: true)))["data"]!;
            var second = (await OkData(await AddMediaAsync(admin, id, Png, "b.png", "image/png", "IMAGE", cover: true)))["data"]!;
            await OkData(await AddMediaAsync(admin, id, Pdf, "kadastr.pdf", "application/pdf", "DOCUMENT"));
            Assert.Equal(HttpStatusCode.BadRequest, (await AddMediaAsync(admin, id, Pdf, "x.pdf", "application/pdf", "IMAGE")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await AddMediaAsync(admin, id, Png, "v.png", "image/png", "VIDEO")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await AddMediaAsync(admin, id, Pdf, "c.pdf", "application/pdf", "DOCUMENT", cover: true)).StatusCode);
            // The refused uploads left no file record behind (rolled back with their transaction).
            Assert.Equal(filesBefore + 3, await api.ScalarAsync<long>("select count(*) from \"UploadedFiles\" where not \"IsDeleted\""));

            var property = (await OkData(await admin.GetAsync($"api/property?Id={id}")))["data"]!;
            var media = property["media"]!.AsArray();
            Assert.Equal(3, media.Count);
            Assert.Single(media, m => m!["isCover"]!.GetValue<bool>());
            Assert.Equal(second["id"]!.GetValue<string>(), media.Single(m => m!["isCover"]!.GetValue<bool>())!["id"]!.GetValue<string>());

            // Moving the cover back to the first image.
            await OkData(await admin.PutAsync("api/property/media", Form(("Id", first["id"]!.GetValue<string>()), ("IsCover", "true"), ("SortOrder", "0"))));
            var item = (await OkData(await PublicListAsync($"Region={region}")))["data"]![0]!;
            Assert.EndsWith(".png", item["cover"]!["url"]!.GetValue<string>());
            Assert.Equal(first["uploadedFile"]!["url"]!.GetValue<string>(), item["cover"]!["url"]!.GetValue<string>());

            var detail = (await OkData(await api.CreateClient().GetAsync($"api/property/public?Id={id}")))["data"]!;
            Assert.Equal(3, detail["media"]!.AsArray().Count);
            Assert.Null(detail["media"]![0]!["file"]!["path"]);   // public: no storage details
        }

        [Fact]
        public async Task The_catalog_filters_searches_sorts_and_pages()
        {
            var admin = await api.AdminClientAsync();
            var suffix = Guid.NewGuid().ToString("N")[..6];
            var parent = await CreateCategoryAsync(admin, "Tijorat " + suffix);
            var child = await CreateCategoryAsync(admin, "Do'kon " + suffix, parent);
            var other = await CreateCategoryAsync(admin, "Yer " + suffix);
            var region = Region();
            await CreatePropertyAsync(admin, [.. PropertyFields(parent, region, area: "40", rooms: "1"), ("District", "Yunusobod")]);
            var big = await CreatePropertyAsync(admin, [.. PropertyFields(child, region, area: "120", rooms: "4", address: "Amir Temur 5")]);
            await CreatePropertyAsync(admin, PropertyFields(other, region, area: "80", rooms: "3"));
            // The middle one is in a scheduled auction.
            var withAuction = await CreatePropertyAsync(admin, PropertyFields(other, region, area: "60", rooms: "2"));
            using (var scope = api.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var start = DateTime.UtcNow.AddDays(3);
                context.Auctions.Add(new Auction
                {
                    LotNumber = "LOT-" + suffix, Title = "Lot", PropertyId = Guid.Parse(withAuction), Status = AuctionStatus.SCHEDULED,
                    StartingPrice = 500_000_000m, MinBidStep = 5_000_000m, DepositAmount = 25_000_000m,
                    ApplicationDeadline = start.AddDays(-1), StartsAt = start, EndsAt = start.AddHours(1),
                });
                await context.SaveChangesAsync();
            }

            Assert.Equal(4, (await OkData(await PublicListAsync($"Region={region}")))["totalCount"]!.GetValue<int>());
            // The parent category includes its subcategory.
            Assert.Equal(2, (await OkData(await PublicListAsync($"Region={region}&CategoryId={parent}")))["totalCount"]!.GetValue<int>());
            Assert.Equal(2, (await OkData(await PublicListAsync($"Region={region}&MinArea=60&MaxArea=100")))["totalCount"]!.GetValue<int>());
            Assert.Equal(2, (await OkData(await PublicListAsync($"Region={region}&MaxRooms=2")))["totalCount"]!.GetValue<int>());
            Assert.Equal(1, (await OkData(await PublicListAsync($"Region={region}&District=YUNUSOBOD")))["totalCount"]!.GetValue<int>());
            Assert.Equal(big, (await OkData(await PublicListAsync($"Region={region}&SearchText=amir%20temur")))["data"]![0]!["id"]!.GetValue<string>());

            var inAuction = await OkData(await PublicListAsync($"Region={region}&OnlyInAuction=true"));
            Assert.Equal(withAuction, inAuction["data"]![0]!["id"]!.GetValue<string>());
            Assert.Equal(500_000_000m, inAuction["data"]![0]!["auction"]!["startingPrice"]!.GetValue<decimal>());
            Assert.Equal(1, (await OkData(await PublicListAsync($"Region={region}&MinPrice=400000000&MaxPrice=600000000")))["totalCount"]!.GetValue<int>());
            Assert.Equal(0, (await OkData(await PublicListAsync($"Region={region}&MaxPrice=100000000")))["totalCount"]!.GetValue<int>());

            var byArea = (await OkData(await PublicListAsync($"Region={region}&Sort=area_asc")))["data"]!.AsArray().Select(x => x!["areaSqm"]!.GetValue<decimal>()).ToList();
            Assert.Equal([40m, 60m, 80m, 120m], byArea);
            // Price sorting: the one with an auction first, the rest after it.
            Assert.Equal(withAuction, (await OkData(await PublicListAsync($"Region={region}&Sort=price_desc")))["data"]![0]!["id"]!.GetValue<string>());

            var page2 = await OkData(await PublicListAsync($"Region={region}&Sort=area_asc&PageSize=3&PageIndex=1"));
            Assert.Equal([120m], page2["data"]!.AsArray().Select(x => x!["areaSqm"]!.GetValue<decimal>()).ToList());
            Assert.Equal(HttpStatusCode.BadRequest, (await PublicListAsync("Sort=cheapest")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await PublicListAsync("PageSize=51")).StatusCode);

            // A property in an auction cannot be deleted; archiving is the way out of the catalog.
            Assert.Equal(HttpStatusCode.Conflict, (await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/property") { Content = JsonContent.Create(new { id = withAuction }) })).StatusCode);
        }

        [Fact]
        public async Task Views_are_counted_and_deleting_a_property_removes_its_media()
        {
            var admin = await api.AdminClientAsync();
            var category = await CreateCategoryAsync(admin, "Dacha " + Guid.NewGuid().ToString("N")[..6]);
            var id = await CreatePropertyAsync(admin, PropertyFields(category, Region()));
            var media = (await OkData(await AddMediaAsync(admin, id, Png, "d.png", "image/png", "IMAGE")))["data"]!;

            await api.CreateClient().GetAsync($"api/property/public?Id={id}");
            var second = (await OkData(await api.CreateClient().GetAsync($"api/property/public?Id={id}")))["data"]!;
            Assert.Equal(2, second["viewCount"]!.GetValue<long>());

            await OkData(await admin.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "api/property") { Content = JsonContent.Create(new { id }) }));
            Assert.True(await api.ScalarAsync<bool>($"select \"IsDeleted\" from \"PropertyMedia\" where \"Id\" = '{media["id"]!.GetValue<string>()}'"));
            Assert.True(await api.ScalarAsync<bool>($"select \"IsDeleted\" from \"UploadedFiles\" where \"Id\" = '{media["uploadedFileId"]!.GetValue<string>()}'"));
            Assert.Equal(HttpStatusCode.NotFound, (await api.CreateClient().GetAsync($"api/property/public?Id={id}")).StatusCode);
        }

        [Fact]
        public async Task Admin_endpoints_need_sign_in_and_the_permissions()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsync("api/property", Form(("Title", "x")))).StatusCode);

            await api.SignUpAsync("plainuser_props");
            var user = api.Authorized(await api.SignInAsync("plainuser_props", "Strong@123"));
            Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("api/property/list")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await user.PostAsync("api/propertycategory", Form(("Name", "x")))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("api/property/public/list")).StatusCode);
        }
    }
}
