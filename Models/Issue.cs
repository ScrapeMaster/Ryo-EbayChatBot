namespace EbayChatBot.API.Models;

public class Issue
{
    public int IssueId { get; set; }
    public int OrderId { get; set; }
    public int BuyerId { get; set; }
    public int UserId { get; set; }

    public string Description { get; set; }
    public string Status { get; set; }  // pending, resolved
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Order Order { get; set; }
    public Buyer Buyer { get; set; }
    public User User { get; set; }
}