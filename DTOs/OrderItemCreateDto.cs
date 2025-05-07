namespace EbayChatBot.API.DTOs
{
    public class OrderItemCreateDto
    {
        public int OrderId { get; set; }  // Existing Order ID
        public string Title { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}