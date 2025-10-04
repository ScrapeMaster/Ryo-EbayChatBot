using Microsoft.AspNetCore.Identity;

namespace EbayChatBot.API.Models;

#nullable disable
public class User : IdentityUser<int>
{
    public string EbayUsername { get; set; }
    public DateTime CreatedAt { get; set; }
}