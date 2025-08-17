namespace EbayChatBot.API.DTOs;

public class FakeReplyDto
{
    public int SellerId { get; set; }
    public int BuyerId { get; set; }
    public string BuyerUsername { get; set; }
    public string Message { get; set; }
}