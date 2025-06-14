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
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.OrderItems)
            .ToListAsync();
    }

    // GET: api/Orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null)
        {
            return NotFound();
        }

        return order;
    }

    // POST: api/Orders
    [HttpPost]
    public async Task<IActionResult> CreateOrder(OrderCreateDto dto)
    {
        var order = new Order
        {
            EbayOrderId = dto.EbayOrderId,
            BuyerId = dto.BuyerId,
            SellerId = dto.SellerId,
            OrderDate = dto.OrderDate,
            Status = dto.Status,
            TotalAmount = dto.TotalAmount
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Send automated message
        await _automatedMessageService.SendOrderPlacedMessageAsync(order.OrderId, AutomatedMessageTrigger.OrderPlaced);
        return Ok(order);
    }


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
        string ebayAuthToken = "v^1.1#i^1#f^0#p^3#I^3#r^0#t^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"; // Replace with actual token logic
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
