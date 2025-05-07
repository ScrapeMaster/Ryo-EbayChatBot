namespace EbayChatBot.API.DTOs;

public class OrderCreateDto
{
    public string EbayOrderId { get; set; }
    public int BuyerId { get; set; }
    public int SellerId { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
}
