using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using EbayChatBot.API.Services;
using EbayChatBot.API.DTOs;


namespace EbayChatBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly EbayChatDbContext _context;
    private readonly AutomatedMessageService _automatedMessageService;
    private readonly EbayOrderService _orderService;

    public OrdersController(
        EbayChatDbContext context,
        AutomatedMessageService automatedMessageService,
        EbayOrderService orderService
    )
    {
        _context = context;
        _automatedMessageService = automatedMessageService;
        _orderService = orderService;
    }

    // GET: api/Orders
    //[HttpGet]
    //public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    //{
    //    return await _context.Orders
    //        .Include(o => o.Buyer)
    //        .Include(o => o.Seller)
    //        .Include(o => o.OrderItems)
    //        .ToListAsync();
    //}

    // GET: api/orders?sellerId=2
    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int sellerId)
    {
        var orders = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.OrderItems)
            .Where(o => o.SellerId == sellerId)
            .Select(o => new
            {
                OrderId = o.OrderId,
                EbayOrderId = o.EbayOrderId,
                BuyerId = o.BuyerId,
                BuyerUsername = o.Buyer != null ? o.Buyer.EbayUsername : null,
                ItemId = o.OrderItems.Select(oi => oi.EbayItemId).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(orders);
    }


    // GET: api/Orders/5
    //[HttpGet("{id}")]
    //public async Task<ActionResult<Order>> GetOrder(int id)
    //{
    //    var order = await _context.Orders
    //        .Include(o => o.Buyer)
    //        .Include(o => o.Seller)
    //        .Include(o => o.OrderItems)
    //        .FirstOrDefaultAsync(o => o.OrderId == id);

    //    if (order == null)
    //    {
    //        return NotFound();
    //    }

    //    return order;
    //}

    // POST: api/Orders
    //[HttpPost]
    //public async Task<IActionResult> CreateOrder(OrderCreateDto dto)
    //{
    //    var order = new Order
    //    {
    //        EbayOrderId = dto.EbayOrderId,
    //        BuyerId = dto.BuyerId,
    //        SellerId = dto.SellerId,
    //        OrderDate = dto.OrderDate,
    //        Status = dto.Status,
    //        TotalAmount = dto.TotalAmount
    //    };

    //    _context.Orders.Add(order);
    //    await _context.SaveChangesAsync();

    //    // Send automated message
    //    await _automatedMessageService.SendOrderPlacedMessageAsync(order.OrderId, AutomatedMessageTrigger.OrderPlaced);
    //    return Ok(order);
    //}


    // PUT: api/Orders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, Order order)
    {
        if (id != order.OrderId)
            return BadRequest();

        _context.Entry(order).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Orders.Any(e => e.OrderId == id))
                return NotFound();
            else
                throw;
        }

        return NoContent();
    }

    // DELETE: api/Orders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrder(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncOrders()
    {
        //TODO
        string ebayAuthToken = "v^1.1#i^1#I^3#p^3#f^0#r^0#t^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"; // Replace with actual token logic
        await _orderService.FetchAndSaveOrdersAsync(ebayAuthToken);
        return Ok("Orders synced successfully.");
    }

    // POST: api/Orders/with-items
    [HttpPost("with-items")]
    public async Task<IActionResult> CreateOrderWithItems(OrderWithItemsCreateDto dto)
    {
        var order = new Order
        {
            EbayOrderId = dto.EbayOrderId,
            BuyerId = dto.BuyerId,
            SellerId = dto.SellerId,
            OrderDate = dto.OrderDate,
            Status = dto.Status,
            TotalAmount = dto.TotalAmount,
            OrderItems = dto.OrderItems.Select(item => new OrderItem
            {
                Title = item.Title,
                SKU = item.SKU,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList()
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Send automated message after creating order
        await _automatedMessageService.SendOrderPlacedMessageAsync(order.OrderId, AutomatedMessageTrigger.OrderPlaced);

        return Ok(order);
    }

}
