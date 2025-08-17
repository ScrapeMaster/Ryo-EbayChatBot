#nullable disable
public class EbayItem
{
    public int Id { get; set; }                // PK in DB
    public string ItemId { get; set; }         // eBay Item ID
    public string Title { get; set; }          // Item title
    public string ListingStatus { get; set; }  // Active, Ended, etc.
    public decimal Price { get; set; }         // Converted from XML string
    public string Currency { get; set; }       // ISO currency code
    public DateTime StartTime { get; set; }    // UTC
    public DateTime EndTime { get; set; }      // UTC
    public string SellerUserId { get; set; }   // Seller’s eBay username
    public string PictureUrl { get; set; }     // Main image
    public string ViewItemUrl { get; set; }    // Link to listing
}
