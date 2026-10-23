using System.Net;

namespace IRieltor.Tests.Integration
{
    /// <summary>W-B08: the site asks which payment methods are switched on, so it does not offer one that would answer 409.</summary>
    [Collection(ApiCollection.Name)]
    public class PaymentProvidersTests(ApiFactory api)
    {
        [Fact]
        public async Task A_signed_in_user_sees_the_methods_that_are_configured()
        {
            await api.SignUpAsync("prov_a1");
            var user = api.Authorized(await api.SignInAsync("prov_a1", "Strong@123"));

            var response = await user.GetAsync("api/payment/providers");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = (await ApiFactory.JsonAsync(response))["data"]!.AsArray().Select(x => x!.GetValue<string>()).ToList();
            // The test setup fills in Payme, Click, the bank and the card.
            Assert.Equal(["PAYME", "CLICK", "BANK_TRANSFER", "CARD_TRANSFER"], list);
        }

        [Fact]
        public async Task Without_signing_in_it_is_401()
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await api.CreateClient().GetAsync("api/payment/providers")).StatusCode);
        }
    }
}
