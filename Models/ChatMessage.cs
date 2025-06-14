namespace EbayChatBot.API.Models;

public class ChatMessage
{
    public int Id { get; set; }

    public SenderType SenderType { get; set; }
    public int SenderId { get; set; }

    public SenderType ReceiverType { get; set; }
    public int ReceiverId { get; set; }

    public string Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Optional Navigation Properties
    public User? SenderUser { get; set; }
    public Buyer? SenderBuyer { get; set; }

    public User? ReceiverUser { get; set; }
    public Buyer? ReceiverBuyer { get; set; }

    public string? ExternalMessageId { get; set; }  // To track eBay messages
    public string? SenderEbayUsername { get; set; }
    public MessageDirection MessageDirection { get; set; }
}

