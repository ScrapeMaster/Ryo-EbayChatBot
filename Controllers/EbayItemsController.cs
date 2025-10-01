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
    public async Task<IActionResult> GetSellerItems()
    {
        string ebayUserID = "f1ambe_158";
        await _ebayItemService.GetSellerItemsAsync(ebayUserID);
        return Ok("Items fetched Successfully");
    }

    [HttpGet("seller/{sellerId}")]
    public async Task<IActionResult> GetItemsBySeller(
    string sellerId,
    int pageNumber = 1,
    int pageSize = 50,
    string? search = null)
    {
        var query = _dbContext.EbayItems
            .Where(e => e.SellerUserId == sellerId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.ItemId.StartsWith(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(e => e.StartTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        });
    }
}