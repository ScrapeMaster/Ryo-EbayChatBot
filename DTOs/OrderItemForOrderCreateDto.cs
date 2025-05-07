namespace EbayChatBot.API.DTOs
{
    public class OrderItemForOrderCreateDto
    {
        public string Title { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderWithItemsCreateDto
    {
        public string EbayOrderId { get; set; }
        public int BuyerId { get; set; }
        public int SellerId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }

        public List<OrderItemForOrderCreateDto> OrderItems { get; set; }
    }
}
