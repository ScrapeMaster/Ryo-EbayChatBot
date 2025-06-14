namespace EbayChatBot.API.Models;

public class Buyer
{
    public int BuyerId { get; set; }
    public string EbayUsername { get; set; }
    public string Email { get; set; }
    public string Country { get; set; }
    public DateTime CreatedAt { get; set; }
}
