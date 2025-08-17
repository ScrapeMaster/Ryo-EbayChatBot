using Microsoft.AspNetCore.Mvc;
using EbayChatBot.API.DTOs;
using EbayChatBot.API.Models;
using EbayChatBot.API.Data;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class EbayMessagesController : ControllerBase
{
    private readonly EbayMessageService _messageService;
    private readonly EbayChatDbContext _dbContext;

    public EbayMessagesController(EbayMessageService messageService, EbayChatDbContext dbContext)
    {
        _messageService = messageService;
        _dbContext = dbContext;
    }


    // Buyers inquiries to seller
    [HttpPost("sync")]
    public async Task<IActionResult> SyncMessages([FromBody] string token)
    {
        await _messageService.SyncMessagesAsync(token);
        return Ok("Messages synced.");
    }

    // Seller message to buyers
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        await _messageService.SendMessageToEbay(dto.Token, dto.ItemId, dto.BuyerUserId, dto.Body, dto.ExternalMessageId);
        return Ok("Message sent.");
    }

    //[HttpPost("fake-reply")]
    //public async Task<IActionResult> FakeReply([FromBody] FakeReplyDto dto)
    //{
    //    var chat = new ChatMessage
    //    {
    //        SenderType = SenderType.Buyer,
    //        SenderEntityId = dto.BuyerId,
    //        ReceiverType = SenderType.User,
    //        ReceiverEntityId = dto.SellerId,
    //        Message = dto.Message,
    //        Timestamp = DateTime.UtcNow,
    //        MessageDirection = MessageDirection.Incoming,
    //        SenderEbayUsername = dto.BuyerUsername,
    //        ExternalMessageId = Guid.NewGuid().ToString() // Just to prevent duplicates
    //    };

    //    _dbContext.ChatMessages.Add(chat);
    //    await _dbContext.SaveChangesAsync();

    //    return Ok("Fake buyer reply stored successfully.");
    //}

    // GET: api/messages?itemId=1234567890
    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages([FromQuery] string itemId)
    {
        // Get order for item
        var orderItem = await _dbContext.OrderItems
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.EbayItemId == itemId);

        if (orderItem == null || orderItem.Order == null)
            return NotFound("Order not found for item.");

        var buyerId = orderItem.Order.BuyerId;
        var sellerId = orderItem.Order.SellerId;

        var messages = await _dbContext.ChatMessages
            .Where(m =>
                (m.SenderEntityId == buyerId && m.ReceiverEntityId == sellerId) ||
                (m.SenderEntityId == sellerId && m.ReceiverEntityId == buyerId))
            .OrderBy(m => m.Timestamp)
            .Select(m => new
            {
                m.Message,
                m.Timestamp,
                m.SenderEbayUsername,
                m.SenderType
            })
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet("buyers/{itemId}")]
    public async Task<IActionResult> GetBuyersForItem(string itemId)
    {
        var buyers = await _dbContext.ChatMessages
            .Where(m => m.ItemId == itemId && m.SenderEbayUsername != null)
            .Select(m => m.SenderEbayUsername)
            .Distinct()
            .ToListAsync();

        return Ok(buyers);
    }

}
