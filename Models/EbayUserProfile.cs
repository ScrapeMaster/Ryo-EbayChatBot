namespace EbayChatBot.API.Models;
#nullable disable
public class EbayUserProfile
{
    public string username { get; set; }
    public BusinessAccount businessAccount { get; set; }
}

public class BusinessAccount { 
    public string email { get; set; }
}
