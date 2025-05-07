using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using EbayChatBot.API.DTOs;

namespace EbayChatBot.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MessageTemplatesController : ControllerBase
{
    private readonly EbayChatDbContext _context;

    public MessageTemplatesController(EbayChatDbContext context)
    {
        _context = context;
    }

    // GET: api/MessageTemplates?userId=1
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageTemplate>>> GetTemplates([FromQuery] int userId)
    {
        return await _context.MessageTemplates
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    // GET: api/MessageTemplates/5
    [HttpGet("{id}")]
    public async Task<ActionResult<MessageTemplate>> GetTemplate(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return template;
    }

    // POST: api/MessageTemplates
    [HttpPost]
    public async Task<ActionResult<MessageTemplate>> CreateTemplate(MessageTemplateDto dto)
    {
        var template = new MessageTemplate
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = dto.UserId
        };

        _context.MessageTemplates.Add(template);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    // PUT: api/MessageTemplates/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, MessageTemplateDto dto)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.Title = dto.Title;
        template.Content = dto.Content;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/MessageTemplates/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        _context.MessageTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
