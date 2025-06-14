namespace EbayChatBot.API.Models;

public class Order
{
    public int OrderId { get; set; } // Internal DB primary key
    public string? EbayOrderId { get; set; } // e.g., "110587291251-10000002137510"

    // These IDs are needed to identify the users in your DB
    public int BuyerId { get; set; }
    public int SellerId { get; set; }

    public DateTime OrderDate { get; set; }
    public string? Status { get; set; } // e.g., "Active"
    public decimal TotalAmount { get; set; } // Total
    public decimal SubtotalAmount { get; set; } // Subtotal
    public decimal ShippingCost { get; set; }

    public string? PaymentStatus { get; set; } // e.g., "Incomplete"
    public string? PaymentMethod { get; set; } // e.g., "None"

    public string? BuyerUserId { get; set; }
    public string? SellerUserId { get; set; }

    public Buyer? Buyer { get; set; }
    public User? Seller { get; set; }

    public ICollection<OrderItem>? OrderItems { get; set; }
}