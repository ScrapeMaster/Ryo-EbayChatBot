using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using EbayChatBot.API.DTOs;

namespace EbayChatBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderItemsController : ControllerBase
    {
        private readonly EbayChatDbContext _context;

        public OrderItemsController(EbayChatDbContext context)
        {
            _context = context;
        }

        // POST: api/OrderItems
        [HttpPost]
        public async Task<IActionResult> CreateOrderItem(OrderItemCreateDto dto)
        {
            var order = await _context.Orders.FindAsync(dto.OrderId);
            if (order == null)
                return NotFound("Order not found.");

            var orderItem = new OrderItem
            {
                OrderId = dto.OrderId,
                Title = dto.Title,
                SKU = dto.SKU,
                Quantity = dto.Quantity,
                Price = dto.Price
            };

            _context.OrderItems.Add(orderItem);
            await _context.SaveChangesAsync();

            return Ok(orderItem);
        }

        // GET: api/OrderItems/ByOrder/5
        [HttpGet("ByOrder/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItem>>> GetOrderItemsByOrderId(int orderId)
        {
            var items = await _context.OrderItems
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            return Ok(items);
        }
    }
}
