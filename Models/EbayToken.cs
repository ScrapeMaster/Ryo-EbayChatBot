public class EbayToken
{
    public int Id { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Optional: Track which user/account this token belongs to
    public string EbayUserId { get; set; }
    public string? Email { get; set; }
}