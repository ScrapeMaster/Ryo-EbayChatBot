using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class EbayTestController : ControllerBase
{
    private readonly EbayOrderService _ebayOrderService;

    public EbayTestController(EbayOrderService ebayOrderService)
    {
        _ebayOrderService = ebayOrderService;
    }

    [HttpGet("sync-orders")]
    public async Task<IActionResult> SyncOrdersManually()
    {
        await _ebayOrderService.SyncOrdersForAllSellersAsync(); // whatever your method name is
        return Ok("Order sync completed successfully");
    }
}
