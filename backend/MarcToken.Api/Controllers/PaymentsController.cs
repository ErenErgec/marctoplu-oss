using MarcToken.Api.Contracts;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/payments/iyzico")]
public sealed class PaymentsController(PaymentService paymentService) : ControllerBase
{
    [HttpPost("checkout/init")]
    [Authorize]
    public async Task<ActionResult<CheckoutInitResponse>> Init([FromBody] CheckoutInitRequest request, CancellationToken ct)
    {
        if (UserContext.IsAnonymous(User))
        {
            return BadRequest(new { message = "Ödeme yapabilmek için lütfen önce hesabınızı e-posta veya Google ile kalıcı bir hesaba dönüştürün." });
        }

        var userId = UserContext.GetUserIdOrThrow(User);
        var email = UserContext.GetEmail(User);
        var (payment, checkoutUrl) = await paymentService.InitCheckoutAsync(userId, email, request, ct);
        return Ok(new CheckoutInitResponse(payment.Id, payment.ProviderConversationId, checkoutUrl));
    }

    [HttpPost("callback")]
    public async Task<ActionResult<string>> Callback([FromForm] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 1024)
        {
            return BadRequest("Gecersiz callback token.");
        }

        try
        {
            await paymentService.FinalizeAndCreditCheckoutAsync(token, ct);
            return Content("Odeme basarili. Token bakiyeniz yuklendi.", "text/plain");
        }
        catch (Exception)
        {
            return BadRequest("Odeme dogrulamasi basarisiz.");
        }
    }
}
