using MarcToken.Api.Contracts;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/token-packages")]
public sealed class TokenPackagesController(PaymentService paymentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<TokenPackageResponse>>> List(CancellationToken ct)
    {
        var packages = await paymentService.GetActivePackagesAsync(ct);
        var result = packages
            .Select(x => new TokenPackageResponse(x.Id, x.Name, x.TokenAmount, x.PriceTry))
            .ToList();
        return Ok(result);
    }
}
