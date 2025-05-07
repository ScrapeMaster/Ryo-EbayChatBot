namespace EbayChatBot.API.DTOs;

public class InquiryCreateDto
{
    public int OrderId { get; set; }
    public int BuyerId { get; set; }
    public int UserId { get; set; }
    public string MessageContent { get; set; }
}
