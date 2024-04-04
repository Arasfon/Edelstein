using Edelstein.PaymentServer.Authorization;

using Microsoft.AspNetCore.Mvc;

namespace Edelstein.PaymentServer.Controllers;

[ApiController]
[Route("/v1.0/payment")]
public class PaymentController : Controller
{
    [Route("productlist")]
    [ServiceFilter<OAuthRsaAuthorizationFilter>]
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
    [ServiceFilter<OAuthRsaAuthorizationFilter>]
    public IActionResult TicketStatus() =>
        Ok(new
        {
            result = "OK",
            entry = Array.Empty<object>()
        });

    [Route("subscription/productlist")]
    [ServiceFilter<OAuthRsaAuthorizationFilter>]
    public IActionResult SubscriptionProductList() =>
        Ok(new
        {
            result = "OK",
            entry = new { products = Array.Empty<object>() }
        });
}
