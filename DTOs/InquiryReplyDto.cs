namespace EbayChatBot.API.DTOs;

public class InquiryReplyDto
{
    public int InquiryId { get; set; }
    public int UserId { get; set; }  // Seller who is replying
    public string ReplyContent { get; set; }
}
