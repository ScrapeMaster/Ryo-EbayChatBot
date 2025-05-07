namespace EbayChatBot.API.Models;

public class SupportTask
{
    public int SupportTaskId { get; set; }
    public int? InquiryId { get; set; }
    public int? IssueId { get; set; }
    public int UserId { get; set; }

    public string Description { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } // pending, completed
    public DateTime CreatedAt { get; set; }

    public Inquiry? Inquiry { get; set; }
    public Issue? Issue { get; set; }
    public User User { get; set; }
}