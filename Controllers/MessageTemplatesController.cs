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

    // GET: api/MessageTemplates?userId=1&search=abc&sort=title
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageTemplate>>> GetTemplates(
        [FromQuery] int userId,
        [FromQuery] string? search,
        [FromQuery] string? sort = "createdAt"
    )
    {
        var query = _context.MessageTemplates.Where(t => t.UserId == userId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.Title.Contains(search));
        }

        query = sort switch
        {
            "title" => query.OrderBy(t => t.Title),
            "createdAt" => query.OrderByDescending(t => t.CreatedAt),
            _ => query
        };

        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MessageTemplate>> GetTemplate(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();
        return template;
    }

    [HttpPost]
    public async Task<ActionResult<MessageTemplate>> CreateTemplate(MessageTemplateDto dto)
    {
        var template = new MessageTemplate
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = dto.UserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.MessageTemplates.Add(template);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTemplate(int id, MessageTemplateDto dto)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.Title = dto.Title;
        template.Content = dto.Content;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        _context.MessageTemplates.Remove(template);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleEnabled(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.IsEnabled = !template.IsEnabled;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(template);
    }

    [HttpPatch("{id}/usage")]
    public async Task<IActionResult> IncrementUsage(int id)
    {
        var template = await _context.MessageTemplates.FindAsync(id);
        if (template == null) return NotFound();

        template.UsageCount += 1;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(template);
    }
}
