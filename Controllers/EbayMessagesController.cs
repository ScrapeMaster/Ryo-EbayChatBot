using Microsoft.AspNetCore.Mvc;
using EbayChatBot.API.DTOs;

[ApiController]
[Route("api/[controller]")]
public class EbayMessagesController : ControllerBase
{
    private readonly EbayMessageService _messageService;

    public EbayMessagesController(EbayMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncMessages([FromBody] string token)
    {
        await _messageService.SyncMessagesAsync(token);
        return Ok("Messages synced.");
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
    {
        await _messageService.SendMessageToEbay(dto.Token, dto.ItemId, dto.BuyerUserId, dto.Body);
        return Ok("Message sent.");
    }
}
