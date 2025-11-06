namespace EbayChatBot.API.DTOs;

public class MessageTemplateDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
}
