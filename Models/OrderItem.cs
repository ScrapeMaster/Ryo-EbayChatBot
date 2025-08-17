using System.Text.Json.Serialization;

namespace EbayChatBot.API.Models;

#nullable disable
public class OrderItem
{
    public int Id { get; set; }
    public string EbayItemId { get; set; }
    public string Title { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Site { get; set; }
    public string SKU { get; set; }

    public int OrderId { get; set; }

    [JsonIgnore]
    public Order Order { get; set; }
}
