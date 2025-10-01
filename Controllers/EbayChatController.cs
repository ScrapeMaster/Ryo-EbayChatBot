using Microsoft.AspNetCore.Mvc;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using EbayChatBot.API.Services;
using EbayChatBot.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EbayChatBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatController : ControllerBase
{
    private readonly EbayChatDbContext _dbContext;
    private readonly EbayMessageService _messageService;


    public ChatController(EbayChatDbContext dbContext, EbayMessageService messageService)
    {
        _dbContext = dbContext;
        _messageService = messageService;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetChatHistory(int user1Id, int user2Id)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m =>
                (m.SenderEntityId == user1Id && m.ReceiverEntityId == user2Id) ||
                (m.SenderEntityId == user2Id && m.ReceiverEntityId == user1Id))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return Ok(messages);
    }

    [HttpGet("{itemId}/{buyerUsername}")]
    public async Task<IActionResult> GetMessagesForBuyer(string itemId, string buyerUsername)
    {
        if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(buyerUsername))
            return BadRequest("ItemId and BuyerUsername are required.");

        var messages = await _dbContext.ChatMessages
            .Where(m => m.ItemId == itemId && m.SenderEbayUsername == buyerUsername)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        if (!messages.Any())
            return NotFound("No messages found for this buyer.");

        return Ok(messages);
    }

    // Seller message to buyers
    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        await _messageService.SendMessageToEbay(dto.Token, dto.ItemId, dto.BuyerUserId, dto.Body, dto.ExternalMessageId);
        return Ok("Message sent.");
    }
}
