using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using static IRieltor.Tests.Integration.AuctionScenario;

namespace IRieltor.Tests.Integration
{
    /// <summary>
    /// P2-07: starting a payment, and the Payme / Click callbacks and the bank transfer that complete it.
    /// The provider requests are built the way the providers' documentation describes them; they are not verified against the
    /// providers' own test environments (that needs merchant accounts).
    /// </summary>
    [Collection(ApiCollection.Name)]
    public class PaymentTests(ApiFactory api)
    {
        private readonly AuctionScenario _scenario = new(api);

        private sealed record Setup(string AuctionId, Applicant Participant);

        /// <summary>An auction with an approved participant who has not paid the deposit yet.</summary>
        private async Task<Setup> UnpaidParticipantAsync(HttpClient admin, decimal deposit = 5_000_000m)
        {
            var id = (await _scenario.PublishedAuctionAsync(admin, deposit: deposit))["id"]!.GetValue<string>();
            return new Setup(id, await _scenario.ParticipantAsync(admin, id, depositPaid: false));
        }

        private async Task<JsonObject> CheckoutAsync(Applicant who, string auctionId, string provider, string purpose = "DEPOSIT")
            => (await Ok(await who.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose, provider })))["data"]!.AsObject();

        private async Task<string> DepositOf(Applicant who) => await api.ScalarAsync<string>($"select \"DepositStatus\" from \"AuctionParticipants\" where \"Id\" = '{who.ParticipantId}'") ?? "";
        private async Task<string> StatusOf(string paymentId) => await api.ScalarAsync<string>($"select \"Status\" from \"Payments\" where \"Id\" = '{paymentId}'") ?? "";

        // ---------------- starting a payment

        [Fact]
        public async Task A_deposit_checkout_gives_the_provider_page_the_bank_details_and_the_same_payment_again()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);

            var payme = await CheckoutAsync(who, auctionId, "PAYME");
            Assert.Equal(5_000_000m, payme["amount"]!.GetValue<decimal>());
            var url = payme["url"]!.GetValue<string>();
            Assert.StartsWith("https://checkout.paycom.uz/", url);
            var encoded = Encoding.UTF8.GetString(Convert.FromBase64String(url["https://checkout.paycom.uz/".Length..]));
            Assert.Equal($"m=test-merchant;ac.order_id={payme["paymentId"]!.GetValue<string>()};a=500000000;c=https://i-rieltor.test/payment/result", encoded);   // the amount in tiyin

            var click = await CheckoutAsync(who, auctionId, "CLICK");
            Assert.Contains("service_id=12345", click["url"]!.GetValue<string>());
            Assert.Contains("merchant_id=777", click["url"]!.GetValue<string>());
            Assert.Contains("amount=5000000.00", click["url"]!.GetValue<string>());
            Assert.Contains($"transaction_param={click["paymentId"]!.GetValue<string>()}", click["url"]!.GetValue<string>());

            var bank = await CheckoutAsync(who, auctionId, "BANK_TRANSFER");
            Assert.Null(bank["url"]);
            Assert.Contains("Test Bank", bank["instructions"]!.GetValue<string>());
            Assert.Contains(bank["paymentId"]!.GetValue<string>(), bank["instructions"]!.GetValue<string>());

            Assert.Equal(payme["paymentId"]!.GetValue<string>(), (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>());

            var mine = (await Ok(await who.Client.GetAsync("api/payment/my")))["data"]!.AsArray();
            Assert.Equal(3, mine.Count);
        }

        [Fact]
        public async Task A_checkout_is_refused_for_strangers_for_unavailable_methods_and_when_nothing_is_due()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var stranger = await api.EimzoUserAsync();
            var free = (await _scenario.PublishedAuctionAsync(admin, deposit: 0))["id"]!.GetValue<string>();
            var freeParticipant = await _scenario.ParticipantAsync(admin, free, depositPaid: false);

            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "DEPOSIT", provider = "PAYME" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await stranger.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "DEPOSIT", provider = "PAYME" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await who.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "DEPOSIT", provider = "UZUM" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await who.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "FINAL_PAYMENT", provider = "PAYME" })).StatusCode);   // not the buyer
            Assert.Equal(HttpStatusCode.BadRequest, (await who.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "REFUND", provider = "PAYME" })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await freeParticipant.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId = free, purpose = "DEPOSIT", provider = "PAYME" })).StatusCode);   // no deposit at all

            await api.ScalarAsync<object>($"update \"AuctionParticipants\" set \"DepositStatus\" = 'PAID' where \"Id\" = '{who.ParticipantId}'");
            Assert.Equal(HttpStatusCode.Conflict, (await who.Client.PostAsJsonAsync("api/payment/checkout", new { auctionId, purpose = "DEPOSIT", provider = "PAYME" })).StatusCode);   // already paid
        }

        // ---------------- bank transfer

        [Fact]
        public async Task A_bank_transfer_counts_when_the_staff_confirm_it_and_a_second_deposit_is_sent_back()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var paymentId = (await CheckoutAsync(who, auctionId, "BANK_TRANSFER"))["paymentId"]!.GetValue<string>();

            // It shows in the list of transfers waiting to be confirmed; ordinary users cannot confirm.
            var waiting = (await Ok(await admin.GetAsync("api/payment/list?Filters%5Bprovider.equals%5D=BANK_TRANSFER&Filters%5Bstatus.equals%5D=PENDING&PageSize=100")))["data"]!.AsArray();
            Assert.Contains(waiting, p => p!["id"]!.GetValue<string>() == paymentId);
            Assert.Equal(HttpStatusCode.Forbidden, (await who.Client.PostAsJsonAsync("api/payment/confirm", new { id = paymentId })).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("api/payment/confirm", new { id = paymentId, reference = "TT-100" })).StatusCode);
            Assert.Equal("PAID", await StatusOf(paymentId));
            Assert.Equal("PAID", await DepositOf(who));
            Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("api/payment/confirm", new { id = paymentId })).StatusCode);

            // A second transfer for the same deposit (the payer paid twice): the money cannot be used and goes back.
            await api.ScalarAsync<object>($"update \"AuctionParticipants\" set \"DepositStatus\" = 'NOT_PAID' where \"Id\" = '{who.ParticipantId}'");
            var second = (await CheckoutAsync(who, auctionId, "BANK_TRANSFER"))["paymentId"]!.GetValue<string>();
            await api.ScalarAsync<object>($"update \"AuctionParticipants\" set \"DepositStatus\" = 'PAID' where \"Id\" = '{who.ParticipantId}'");
            await Ok(await admin.PostAsJsonAsync("api/payment/confirm", new { id = second }));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"Payments\" where \"Purpose\" = 'REFUND' and \"Note\" = 'Qaytarish: to''lov {second}'"));
        }

        [Fact]
        public async Task The_staff_can_enter_a_payment_by_hand_for_the_exact_amount_only()
        {
            var admin = await api.AdminClientAsync();
            var (_, who) = await UnpaidParticipantAsync(admin);

            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/payment/manual", new { participantId = who.ParticipantId, purpose = "DEPOSIT", amount = 4_000_000, provider = "BANK_TRANSFER" })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("api/payment/manual", new { participantId = who.ParticipantId, purpose = "DEPOSIT", amount = 5_000_000, provider = "PAYME" })).StatusCode);   // Payme reports its own
            Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("api/payment/manual", new { participantId = who.ParticipantId, purpose = "DEPOSIT", amount = 5_000_000, provider = "MANUAL", note = "Kassaga naqd" })).StatusCode);
            Assert.Equal("PAID", await DepositOf(who));
            Assert.Equal(HttpStatusCode.Forbidden, (await who.Client.PostAsJsonAsync("api/payment/manual", new { participantId = who.ParticipantId, purpose = "DEPOSIT", amount = 5_000_000, provider = "MANUAL" })).StatusCode);
        }

        // ---------------- Payme

        private async Task<JsonObject> PaymeAsync(string method, object parameters, string? key = ApiFactory.PaymeKey, int id = 7)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/payments/payme") { Content = JsonContent.Create(new { method, @params = parameters, id }) };
            if (key != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("Paycom:" + key)));
            var response = await api.CreateClient().SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // Payme reads the error inside the body
            var body = await ApiFactory.JsonAsync(response);
            Assert.Equal(id, body["id"]!.GetValue<int>());
            return body;
        }

        private static int ErrorCode(JsonObject body) => body["error"]!["code"]!.GetValue<int>();
        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private static object Account(string paymentId) => new { order_id = paymentId };

        [Fact]
        public async Task Payme_must_know_the_key_and_the_order_and_the_amount()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var paymentId = (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>();

            Assert.Equal(-32504, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(paymentId) }, key: null)));
            Assert.Equal(-32504, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(paymentId) }, key: "wrong")));
            Assert.Equal(-31050, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(Guid.NewGuid().ToString()) })));
            Assert.Equal(-31050, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = new { order_id = "not a guid" } })));
            Assert.Equal(-31001, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 499_999_999, account = Account(paymentId) })));
            Assert.Equal(-32601, ErrorCode(await PaymeAsync("Frobnicate", new { })));
            Assert.True((await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(paymentId) }))["result"]!["allow"]!.GetValue<bool>());

            // An order meant for Click cannot be paid through Payme.
            var click = (await CheckoutAsync(who, auctionId, "CLICK"))["paymentId"]!.GetValue<string>();
            Assert.Equal(-31051, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(click) })));
        }

        [Fact]
        public async Task A_Payme_payment_goes_through_create_perform_and_check_and_makes_the_deposit_paid()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var paymentId = (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>();
            var transaction = "payme-" + Guid.NewGuid().ToString("N")[..12];
            var created = Now();

            var create = (await PaymeAsync("CreateTransaction", new { id = transaction, time = created, amount = 500_000_000, account = Account(paymentId) }))["result"]!;
            Assert.Equal(created, create["create_time"]!.GetValue<long>());
            Assert.Equal(1, create["state"]!.GetValue<int>());
            Assert.Equal(paymentId.Replace("-", ""), create["transaction"]!.GetValue<string>());
            Assert.Equal("NOT_PAID", await DepositOf(who));

            // The same create again changes nothing; a second Payme transaction for the order is refused while the first waits.
            Assert.Equal(created, (await PaymeAsync("CreateTransaction", new { id = transaction, time = Now(), amount = 500_000_000, account = Account(paymentId) }))["result"]!["create_time"]!.GetValue<long>());
            Assert.Equal(-31099, ErrorCode(await PaymeAsync("CreateTransaction", new { id = "other-" + transaction, time = Now(), amount = 500_000_000, account = Account(paymentId) })));

            var perform = (await PaymeAsync("PerformTransaction", new { id = transaction }))["result"]!;
            Assert.Equal(2, perform["state"]!.GetValue<int>());
            var performTime = perform["perform_time"]!.GetValue<long>();
            Assert.InRange(performTime, created, Now());
            Assert.Equal("PAID", await StatusOf(paymentId));
            Assert.Equal("PAID", await DepositOf(who));

            // Repeated perform: the same answer. The check shows the whole story.
            Assert.Equal(performTime, (await PaymeAsync("PerformTransaction", new { id = transaction }))["result"]!["perform_time"]!.GetValue<long>());
            var check = (await PaymeAsync("CheckTransaction", new { id = transaction }))["result"]!;
            Assert.Equal(2, check["state"]!.GetValue<int>());
            Assert.Equal(created, check["create_time"]!.GetValue<long>());
            Assert.Equal(performTime, check["perform_time"]!.GetValue<long>());
            Assert.Equal(0, check["cancel_time"]!.GetValue<long>());

            // A paid order cannot be cancelled by Payme; and it is no longer payable.
            Assert.Equal(-31007, ErrorCode(await PaymeAsync("CancelTransaction", new { id = transaction, reason = 5 })));
            Assert.Equal(-31051, ErrorCode(await PaymeAsync("CheckPerformTransaction", new { amount = 500_000_000, account = Account(paymentId) })));
            Assert.Equal(-31003, ErrorCode(await PaymeAsync("PerformTransaction", new { id = "unknown" })));

            var statement = (await PaymeAsync("GetStatement", new { from = created - 1000, to = Now() + 1000 }))["result"]!["transactions"]!.AsArray();
            var line = Assert.Single(statement, t => t!["id"]!.GetValue<string>() == transaction)!;
            Assert.Equal(500_000_000, line["amount"]!.GetValue<long>());
            Assert.Equal(paymentId, line["account"]!["order_id"]!.GetValue<string>());
        }

        [Fact]
        public async Task A_Payme_transaction_can_be_cancelled_before_it_is_performed_and_times_out_after_twelve_hours()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var first = (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>();
            var transaction = "payme-c-" + Guid.NewGuid().ToString("N")[..10];
            await PaymeAsync("CreateTransaction", new { id = transaction, time = Now(), amount = 500_000_000, account = Account(first) });

            var cancel = (await PaymeAsync("CancelTransaction", new { id = transaction, reason = 3 }))["result"]!;
            Assert.Equal(-1, cancel["state"]!.GetValue<int>());
            Assert.Equal("CANCELLED", await StatusOf(first));
            Assert.Equal(-1, (await PaymeAsync("CancelTransaction", new { id = transaction, reason = 3 }))["result"]!["state"]!.GetValue<int>());   // again: the same
            Assert.Equal(-31008, ErrorCode(await PaymeAsync("PerformTransaction", new { id = transaction })));
            Assert.Equal("NOT_PAID", await DepositOf(who));

            // A new payment can be started, and one left waiting too long is cancelled when Payme finally performs it.
            var second = (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>();
            var slow = "payme-t-" + Guid.NewGuid().ToString("N")[..10];
            await PaymeAsync("CreateTransaction", new { id = slow, time = Now(), amount = 500_000_000, account = Account(second) });
            await api.ScalarAsync<object>($"update \"Payments\" set \"ProviderCreatedAt\" = now() - interval '13 hours' where \"Id\" = '{second}'");

            Assert.Equal(-31008, ErrorCode(await PaymeAsync("PerformTransaction", new { id = slow })));
            Assert.Equal("CANCELLED", await StatusOf(second));
            Assert.Equal(4, await api.ScalarAsync<int>($"select \"ProviderReason\" from \"Payments\" where \"Id\" = '{second}'"));
            Assert.Equal("NOT_PAID", await DepositOf(who));
        }

        [Fact]
        public async Task The_same_Payme_perform_arriving_many_times_at_once_settles_the_payment_once()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            await api.ScalarAsync<object>($"update \"Users\" set \"Phone\" = '+998905550001' where \"Id\" = '{who.UserId}'");
            var paymentId = (await CheckoutAsync(who, auctionId, "PAYME"))["paymentId"]!.GetValue<string>();
            var transaction = "payme-p-" + Guid.NewGuid().ToString("N")[..10];
            await PaymeAsync("CreateTransaction", new { id = transaction, time = Now(), amount = 500_000_000, account = Account(paymentId) });

            var answers = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => PaymeAsync("PerformTransaction", new { id = transaction })));

            Assert.All(answers, a => Assert.Equal(2, a["result"]!["state"]!.GetValue<int>()));
            Assert.Equal(1L, await api.ScalarAsync<long>($"select count(*) from \"Notifications\" where \"UserId\" = '{who.UserId}' and \"Type\" = 'DEPOSIT_RECEIVED'"));
            Assert.Equal("PAID", await DepositOf(who));
        }

        // ---------------- Click

        private static string Md5(string text) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

        private async Task<JsonObject> ClickAsync(string step, Dictionary<string, string> form, bool sign = true)
        {
            if (sign)
            {
                var prepareId = step == "complete" ? form["merchant_prepare_id"] : "";
                form["sign_string"] = Md5(form["click_trans_id"] + form["service_id"] + ApiFactory.ClickSecret + form["merchant_trans_id"] + prepareId + form["amount"] + form["action"] + form["sign_time"]);
            }
            var response = await api.CreateClient().PostAsync($"api/payments/click/{step}", new FormUrlEncodedContent(form));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ApiFactory.JsonAsync(response);
        }

        private static Dictionary<string, string> ClickForm(string clickTransId, string paymentId, string amount, string action, string? prepareId = null, string error = "0")
        {
            var form = new Dictionary<string, string>
            {
                ["click_trans_id"] = clickTransId, ["service_id"] = ApiFactory.ClickServiceId, ["click_paydoc_id"] = "9001",
                ["merchant_trans_id"] = paymentId, ["amount"] = amount, ["action"] = action, ["error"] = error, ["error_note"] = "",
                ["sign_time"] = "2026-10-22 10:00:00",
            };
            if (prepareId != null)
                form["merchant_prepare_id"] = prepareId;
            return form;
        }

        [Fact]
        public async Task A_Click_payment_goes_through_prepare_and_complete_and_makes_the_deposit_paid()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var paymentId = (await CheckoutAsync(who, auctionId, "CLICK"))["paymentId"]!.GetValue<string>();
            var clickId = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000).ToString();

            // Signature, amount, order and action are all checked.
            Assert.Equal(-1, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0"), sign: false))["error"]!.GetValue<int>());
            var wrongSign = ClickForm(clickId, paymentId, "5000000.00", "0"); wrongSign["sign_string"] = "deadbeef";
            Assert.Equal(-1, (await ClickAsync("prepare", wrongSign, sign: false))["error"]!.GetValue<int>());
            Assert.Equal(-2, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "4999999.00", "0")))["error"]!.GetValue<int>());
            Assert.Equal(-5, (await ClickAsync("prepare", ClickForm(clickId, Guid.NewGuid().ToString(), "5000000.00", "0")))["error"]!.GetValue<int>());
            Assert.Equal(-3, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "1")))["error"]!.GetValue<int>());

            var prepare = await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0"));
            Assert.Equal(0, prepare["error"]!.GetValue<int>());
            var prepareId = prepare["merchant_prepare_id"]!.GetValue<long>().ToString();
            Assert.Equal(paymentId, prepare["merchant_trans_id"]!.GetValue<string>());
            // A repeated prepare keeps its id.
            Assert.Equal(prepareId, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0")))["merchant_prepare_id"]!.GetValue<long>().ToString());
            Assert.Equal("NOT_PAID", await DepositOf(who));

            // Complete needs the right transaction and prepare id.
            Assert.Equal(-6, (await ClickAsync("complete", ClickForm(clickId, paymentId, "5000000.00", "1", prepareId: "12345")))["error"]!.GetValue<int>());
            Assert.Equal(-6, (await ClickAsync("complete", ClickForm("999", paymentId, "5000000.00", "1", prepareId: prepareId)))["error"]!.GetValue<int>());

            var complete = await ClickAsync("complete", ClickForm(clickId, paymentId, "5000000.00", "1", prepareId: prepareId));
            Assert.Equal(0, complete["error"]!.GetValue<int>());
            Assert.Equal(prepareId, complete["merchant_confirm_id"]!.GetValue<long>().ToString());
            Assert.Equal("PAID", await StatusOf(paymentId));
            Assert.Equal("PAID", await DepositOf(who));

            // Once paid: "already paid" for both steps.
            Assert.Equal(-4, (await ClickAsync("complete", ClickForm(clickId, paymentId, "5000000.00", "1", prepareId: prepareId)))["error"]!.GetValue<int>());
            Assert.Equal(-4, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0")))["error"]!.GetValue<int>());
        }

        [Fact]
        public async Task A_Click_payment_that_Click_could_not_collect_is_cancelled()
        {
            var admin = await api.AdminClientAsync();
            var (auctionId, who) = await UnpaidParticipantAsync(admin);
            var paymentId = (await CheckoutAsync(who, auctionId, "CLICK"))["paymentId"]!.GetValue<string>();
            var clickId = Random.Shared.NextInt64(1_000_000_000, 9_000_000_000).ToString();
            var prepareId = (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0")))["merchant_prepare_id"]!.GetValue<long>().ToString();

            var failed = await ClickAsync("complete", ClickForm(clickId, paymentId, "5000000.00", "1", prepareId: prepareId, error: "-5017"));

            Assert.Equal(-9, failed["error"]!.GetValue<int>());
            Assert.Equal("CANCELLED", await StatusOf(paymentId));
            Assert.Equal("NOT_PAID", await DepositOf(who));
            Assert.Equal(-9, (await ClickAsync("prepare", ClickForm(clickId, paymentId, "5000000.00", "0")))["error"]!.GetValue<int>());
        }

        [Fact]
        public async Task The_buyer_pays_the_final_payment_through_a_provider_and_the_property_is_sold()
        {
            var admin = await api.AdminClientAsync();
            var auction = await _scenario.PublishedAuctionAsync(admin);
            var id = auction["id"]!.GetValue<string>();
            var buyer = await _scenario.ParticipantAsync(admin, id);
            using (var scope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(api.Services))
            {
                var lifecycle = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Application.Interfaces.Auctions.IAuctionLifecycleService>(scope.ServiceProvider);
                await lifecycle.RunOnceAsync(Time(auction["startsAt"]).AddSeconds(1));
                await Ok(await buyer.Client.PostAsJsonAsync("api/bid", new { auctionId = id, amount = 120_000_000 }));
                await lifecycle.RunOnceAsync(Time(auction["endsAt"]).AddSeconds(1));
            }

            var checkout = await CheckoutAsync(buyer, id, "PAYME", "FINAL_PAYMENT");
            Assert.Equal(115_000_000m, checkout["amount"]!.GetValue<decimal>());   // 120 million less the 5 million deposit
            var paymentId = checkout["paymentId"]!.GetValue<string>();
            var transaction = "payme-f-" + Guid.NewGuid().ToString("N")[..10];
            await PaymeAsync("CreateTransaction", new { id = transaction, time = Now(), amount = 11_500_000_000L, account = Account(paymentId) });
            await PaymeAsync("PerformTransaction", new { id = transaction });

            Assert.Equal("SOLD", await api.ScalarAsync<string>($"select \"Outcome\" from \"AuctionResults\" where \"AuctionId\" = '{id}'"));
            Assert.Equal("SOLD", await api.ScalarAsync<string>($"select \"Status\" from \"Properties\" where \"Id\" = '{auction["propertyId"]!.GetValue<string>()}'"));
        }
    }
}
