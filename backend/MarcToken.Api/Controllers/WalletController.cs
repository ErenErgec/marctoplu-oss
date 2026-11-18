using MarcToken.Api.Contracts;
using MarcToken.Api.Infrastructure;
using MarcToken.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarcToken.Api.Controllers;

[ApiController]
[Route("api/me/wallet")]
[Authorize]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WalletController(TokenService tokenService, IWebHostEnvironment env) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WalletResponse>> Get(CancellationToken ct)
    {
        var userId = UserContext.GetUserIdOrThrow(User);
        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
        var rawIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        var ip = !string.IsNullOrWhiteSpace(rawIp)
            ? rawIp.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0]
            : HttpContext.Connection.RemoteIpAddress?.ToString();

        var balance = await tokenService.GetBalanceAsync(userId, deviceId, ip, ct);
        return Ok(new WalletResponse(balance));
    }

    [HttpPost("add-test")]
    public async Task<IActionResult> AddTestTokens([FromQuery] int amount, CancellationToken ct)
    {
        if (!env.IsDevelopment())
        {
            return NotFound();
        }

        if (amount is < 1 or > 1000)
        {
            return BadRequest(new { message = "amount must be between 1 and 1000." });
        }

        var userId = UserContext.GetUserIdOrThrow(User);
        await tokenService.CreditAsync(userId, null, null, amount, "test_credit", ct);
        return Ok(new { success = true, addedAmount = amount });
    }
}
