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

    //[HttpPost("sync")]
    //public async Task<IActionResult> SyncOrders()
    //{
    //    string ebayUserID = "f1ambe_158";
    //    await _orderService.FetchAndSaveOrdersAsync(ebayUserID);
    //    return Ok("Orders synced successfully.");
    //}

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
        //await _automatedMessageService.SendOrderPlacedMessageAsync(order.OrderId, AutomatedMessageTrigger.OrderPlaced);

        return Ok(order);
    }

}
