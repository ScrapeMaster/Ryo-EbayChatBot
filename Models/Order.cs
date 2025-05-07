namespace EbayChatBot.API.Models;

public class Order
{
    public int OrderId { get; set; }
    public string EbayOrderId { get; set; }
    public int BuyerId { get; set; }
    public int SellerId { get; set; }  // User ID

    public DateTime OrderDate { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }

    public Buyer Buyer { get; set; }
    public User Seller { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; }
}