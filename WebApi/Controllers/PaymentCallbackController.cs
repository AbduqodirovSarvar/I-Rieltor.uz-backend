using Application.Interfaces.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace WebApi.Controllers
{
    /// <summary>
    /// Where Payme and Click call us. Not for browsers: no sign-in, no CORS; the request proves itself (Payme's key in the
    /// Authorization header, Click's MD5 signature). The answers always have HTTP status 200: the providers read the error
    /// code inside the body.
    /// </summary>
    [Route("api/payments")]
    [ApiController]
    [DisableRateLimiting]
    public class PaymentCallbackController(IPaymeMerchantService payme, IClickShopService click) : ControllerBase
    {
        /// <summary>Payme Merchant API endpoint (JSON-RPC 2.0).</summary>
        [HttpPost("payme")]
        public async Task<IActionResult> Payme([FromBody] JsonElement request)
        {
            var answer = await payme.HandleAsync(request, Request.Headers.Authorization.ToString(), HttpContext.RequestAborted);
            // The JSON as built (snake_case names such as create_time), not as the API's naming policy would write it.
            return Content(answer.ToJsonString(), "application/json");
        }

        [HttpPost("click/prepare")]
        public Task<IActionResult> ClickPrepare([FromForm] IFormCollection form) => Click(ClickStep.Prepare, form);

        [HttpPost("click/complete")]
        public Task<IActionResult> ClickComplete([FromForm] IFormCollection form) => Click(ClickStep.Complete, form);

        private async Task<IActionResult> Click(ClickStep step, IFormCollection form)
        {
            var fields = form.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
            var answer = await click.HandleAsync(step, fields, HttpContext.RequestAborted);
            return Content(answer.ToJsonString(), "application/json");
        }
    }
}
