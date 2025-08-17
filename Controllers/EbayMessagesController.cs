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
        string token2 = "v^1.1#i^1#p^3#r^0#I^3#f^0#t^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";
        await _messageService.SyncMessagesAsync(token2);
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
