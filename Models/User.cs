namespace EbayChatBot.API.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }  // Admin, Agent, etc.
    public DateTime CreatedAt { get; set; }
    public string EbayUsername { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; }
}