using Edelstein.Data.Extensions;
using Edelstein.Data.Models;
using Edelstein.Data.Transport;
using Edelstein.PaymentServer.Authorization;
using Edelstein.PaymentServer.Services;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.PaymentServer.Controllers;

[ApiController]
[Route("/v1.0/payment")]
[ServiceFilter<OAuthRsaAuthorizationFilter>]
public class PaymentController : Controller
{
    private readonly IUserService _userService;

    public PaymentController(IUserService userService) =>
        _userService = userService;

    [Route("productlist")]
    public IActionResult ProductList() =>
        Ok(new
        {
            result = "OK",
            entry = new
            {
                products = Array.Empty<object>(),
                welcome = "0"
            }
        });

    [Route("ticket/status")]
    public IActionResult TicketStatus() =>
        Ok(new
        {
            result = "OK",
            entry = Array.Empty<object>()
        });

    [Route("subscription/productlist")]
    public IActionResult SubscriptionProductList() =>
        Ok(new
        {
            result = "OK",
            entry = new { products = Array.Empty<object>() }
        });

    [Route("balance")]
    public async Task<IActionResult> Balance()
    {
        ulong xuid = User.FindFirst(ClaimNames.Xuid).As<ulong>();

        UserData user = await _userService.GetUserDataByXuid(xuid);

        return Ok(new
        {
            result = "OK",
            entry = new
            {
                balance_charge_gem = user.Gem.Charge,
                balance_free_gem = user.Gem.Free,
                balance_total_gem = user.Gem.Total
            }
        });
    }
}
