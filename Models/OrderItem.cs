using System.Text.Json.Serialization;

namespace EbayChatBot.API.Models;

public class OrderItem
{
    public int OrderItemId { get; set; }
    public int OrderId { get; set; }  // FK to Order

    public string Title { get; set; }
    public string SKU { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }

    [JsonIgnore]
    public Order Order { get; set; }  // Navigation property
}
