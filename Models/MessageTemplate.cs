namespace EbayChatBot.API.Models;

public class MessageTemplate
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int UsageCount { get; set; } = 0;
    public User User { get; set; }
}
