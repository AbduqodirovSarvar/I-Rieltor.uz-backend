using System.Net;
using System.Net.Http.Json;

namespace IRieltor.Tests.Integration
{
    /// <summary>W-B02: the contact form of the public site. The test host sends the message to Contact:Email.</summary>
    [Collection(ApiCollection.Name)]
    public class ContactTests(ApiFactory api)
    {
        private const string Office = "office@tests.local";

        private Task<HttpResponseMessage> SendAsync(object body) => api.CreateClient().PostAsJsonAsync("api/contact", body);

        private string[] Received(string marker)
            => api.Sinks.EmailsTo(Office).Where(e => e.Html.Contains(marker)).Select(e => e.Html).ToArray();

        [Fact]
        public async Task A_message_is_mailed_to_the_company_with_the_senders_details_encoded()
        {
            var response = await SendAsync(new { name = "Ali  Valiyev", phone = "+998901234567", email = "ali@tests.local", message = "Salom <script>alert(1)</script> MARKER-A1 narx haqida" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var mail = Assert.Single(Received("MARKER-A1"));
            Assert.Contains("Ali Valiyev", mail);
            Assert.Contains("+998901234567", mail);
            Assert.Contains("ali@tests.local", mail);
            Assert.DoesNotContain("<script>", mail);
            Assert.Contains("&lt;script&gt;", mail);
        }

        [Fact]
        public async Task The_trap_field_filled_in_looks_like_success_and_sends_nothing()
        {
            var response = await SendAsync(new { name = "Robot", email = "bot@tests.local", message = "MARKER-B1 buy cheap things now", website = "http://spam.example" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty(Received("MARKER-B1"));
        }

        [Fact]
        public async Task Missing_or_bad_input_is_400_and_a_way_to_answer_is_required()
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(new { name = "", email = "a@tests.local", message = "Yetarlicha uzun xabar" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(new { name = "Ali", email = "a@tests.local", message = "qisqa" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(new { name = "Ali", email = "not-an-email", message = "Yetarlicha uzun xabar" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(new { name = "Ali", phone = "998901112233", message = "Yetarlicha uzun xabar" })).StatusCode);
            // No phone and no e-mail: nobody to answer.
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(new { name = "Ali", message = "MARKER-C1 yetarlicha uzun xabar" })).StatusCode);
            Assert.Empty(Received("MARKER-C1"));
        }

        [Fact]
        public async Task The_same_person_cannot_send_more_than_five_a_day()
        {
            for (var i = 1; i <= 5; i++)
                Assert.Equal(HttpStatusCode.OK, (await SendAsync(new { name = "Vali", email = "spammer@tests.local", message = $"MARKER-D{i} yetarlicha uzun xabar" })).StatusCode);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await SendAsync(new { name = "Vali", email = "spammer@tests.local", message = "MARKER-D6 yetarlicha uzun xabar" })).StatusCode);
            Assert.Empty(Received("MARKER-D6"));
        }

        [Fact]
        public async Task A_mail_server_that_is_down_is_503_so_the_person_knows_to_try_again()
        {
            api.Sinks.EmailFails = true;
            try
            {
                Assert.Equal(HttpStatusCode.ServiceUnavailable, (await SendAsync(new { name = "Ali", email = "down@tests.local", message = "MARKER-E1 yetarlicha uzun xabar" })).StatusCode);
            }
            finally { api.Sinks.EmailFails = false; }
        }
    }
}
