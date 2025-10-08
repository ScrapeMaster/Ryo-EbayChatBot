namespace EbayChatBot.API.DTOs;

public class SendMessageDto
{
    public string EbayUsername { get; set; }
    public string ItemId { get; set; }
    public string BuyerUserId { get; set; }
    public string Body { get; set; }
    public string ExternalMessageId { get; set; }
}