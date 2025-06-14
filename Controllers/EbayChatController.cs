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

    public ChatController(EbayChatDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetChatHistory(int user1Id, int user2Id)
    {
        var messages = await _dbContext.ChatMessages
            .Where(m =>
                (m.SenderId == user1Id && m.ReceiverId == user2Id) ||
                (m.SenderId == user2Id && m.ReceiverId == user1Id))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return Ok(messages);
    }
}
