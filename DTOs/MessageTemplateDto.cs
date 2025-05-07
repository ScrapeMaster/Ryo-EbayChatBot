namespace EbayChatBot.API.DTOs;

public class MessageTemplateDto
{
    public string Title { get; set; }
    public string Content { get; set; }
    public int UserId { get; set; }  // Seller ID
}
