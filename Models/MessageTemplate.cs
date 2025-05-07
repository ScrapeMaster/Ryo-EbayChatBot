namespace EbayChatBot.API.Models;

public class MessageTemplate
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string Title { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; }
}
