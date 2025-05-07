namespace EbayChatBot.API.Models;

public class Inquiry
{
    public int InquiryId { get; set; }
    public int OrderId { get; set; }
    public int BuyerId { get; set; }
    public int UserId { get; set; }
    public bool IsAutomated { get; set; }

    public string MessageContent { get; set; }
    public string? TranslatedContent { get; set; }
    public string Status { get; set; }  // open, resolved
    public DateTime Timestamp { get; set; }

    public Order Order { get; set; }
    public Buyer Buyer { get; set; }
    public User User { get; set; }
}