namespace EbayChatBot.API.Models;

public class Team
{
    public int TeamId { get; set; }
    public string TeamName { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<User> Users { get; set; }
}