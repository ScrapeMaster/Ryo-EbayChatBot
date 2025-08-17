using Microsoft.AspNetCore.Mvc;
using EbayChatBot.API.Services;
using EbayChatBot.API.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class EbayItemsController : ControllerBase
{
    private readonly EbayItemService _ebayItemService;
    private readonly EbayChatDbContext _dbContext;


    public EbayItemsController(EbayItemService ebayItemService, EbayChatDbContext dbContext)
    {
        _ebayItemService = ebayItemService;
        _dbContext = dbContext;
    }

    [HttpGet("sync")]
    public async Task<IActionResult> GetSellerItems([FromHeader] string ebayAuthToken)
    {
        await _ebayItemService.GetSellerItemsAsync(ebayAuthToken);
        return Ok("Items fetched Successfully");
    }

    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> GetItemsBySeller(string sellerId)
    {
        if (string.IsNullOrWhiteSpace(sellerId))
            return BadRequest("SellerID is required.");

        var items = await _dbContext.EbayItems
            .Where(e => e.SellerUserId == sellerId)
            //.Take(100)
            .ToListAsync();

        if (!items.Any())
            return NotFound($"No items found for seller {sellerId}.");

        return Ok(items);
    }

}