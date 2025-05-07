using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Data;
using EbayChatBot.API.DTOs;
using EbayChatBot.API.Models;

namespace EbayChatBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InquiryController : ControllerBase
{
    private readonly EbayChatDbContext _context;

    public InquiryController(EbayChatDbContext context)
    {
        _context = context;
    }

    // GET: api/Inquiry
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Inquiry>>> GetInquiries()
    {
        return await _context.Inquiries
            .Include(i => i.Buyer)
            .Include(i => i.User)
            .Include(i => i.Order)
            .ToListAsync();
    }

    // POST: api/Inquiry
    [HttpPost]
    public async Task<IActionResult> CreateInquiry(InquiryCreateDto dto)
    {
        var inquiry = new Inquiry
        {
            OrderId = dto.OrderId,
            BuyerId = dto.BuyerId,
            UserId = dto.UserId,
            MessageContent = dto.MessageContent,
            Timestamp = DateTime.UtcNow,
            Status = "open",
            IsAutomated = false
        };

        _context.Inquiries.Add(inquiry);
        await _context.SaveChangesAsync();

        return Ok(inquiry);
    }

    // POST: api/Inquiry/reply
    [HttpPost("reply")]
    public async Task<IActionResult> ReplyToInquiry(InquiryReplyDto dto)
    {
        var inquiry = await _context.Inquiries.FindAsync(dto.InquiryId);
        if (inquiry == null)
            return NotFound();

        var reply = new Inquiry
        {
            OrderId = inquiry.OrderId,
            BuyerId = inquiry.BuyerId,
            UserId = dto.UserId,
            MessageContent = dto.ReplyContent,
            Timestamp = DateTime.UtcNow,
            Status = "open",
            IsAutomated = false
        };

        _context.Inquiries.Add(reply);
        await _context.SaveChangesAsync();

        return Ok(reply);
    }
}
