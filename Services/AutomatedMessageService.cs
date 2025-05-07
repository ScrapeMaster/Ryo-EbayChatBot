using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Models;
using EbayChatBot.API.Data;

namespace EbayChatBot.API.Services;

public class AutomatedMessageService
{
    private readonly EbayChatDbContext _context;

    public AutomatedMessageService(EbayChatDbContext context)
    {
        _context = context;
    }

    public async Task SendOrderPlacedMessageAsync(int orderId, AutomatedMessageTrigger trigger)
    {
        var order = await _context.Orders
            .Include(o => o.Buyer)   // Ensure we have buyer details
            .Include(o => o.Seller)  // Ensure we have seller details
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null) return;

        // Extract the trigger to find the right template
        var keyword = trigger.ToString(); // Example: "OrderPlaced"

        // Find the template that matches the trigger for the seller
        var template = await _context.MessageTemplates
            .Where(t => t.UserId == order.SellerId && t.Title.Contains(keyword))
            .FirstOrDefaultAsync();

        if (template == null) return; // No matching template, skip

        // Customize the message content if needed, e.g., include product name
        var messageContent = template.Content.Replace("{OrderId}", order.EbayOrderId);

        // Create a new inquiry with the automated message
        var inquiry = new Inquiry
        {
            OrderId = orderId,
            BuyerId = order.BuyerId,
            UserId = order.SellerId,
            MessageContent = messageContent,
            Status = "open",
            Timestamp = DateTime.UtcNow,
            IsAutomated = true
        };

        // Save the inquiry to the database
        _context.Inquiries.Add(inquiry);
        await _context.SaveChangesAsync();
    }
}
