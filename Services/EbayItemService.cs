using System.Xml.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using EbayChatBot.API.Services;

public class EbayItemService
{
    private readonly HttpClient _httpClient;
    private readonly EbayChatDbContext _dbContext;
    private readonly EbayOAuthService _ebayOAuthService;

    public EbayItemService(EbayChatDbContext dbContext, HttpClient httpClient, EbayOAuthService ebayOAuthService)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _ebayOAuthService = ebayOAuthService;
    }

    public async Task GetSellerItemsAsync(string ebayAuthToken)
    {
        int pageNumber = 1;
        int entriesPerPage = 100;
        bool morePages = true;

        while (morePages)
        {
            var requestXml = $@"
        <?xml version=""1.0"" encoding=""utf-8""?>
        <GetSellerListRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
          <RequesterCredentials>
            <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
          </RequesterCredentials>
          <StartTimeFrom>{DateTime.UtcNow.AddDays(-120):yyyy-MM-ddTHH:mm:ss.fffZ}</StartTimeFrom>
          <StartTimeTo>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}</StartTimeTo>
          <Pagination>
            <EntriesPerPage>{entriesPerPage}</EntriesPerPage>
            <PageNumber>{pageNumber}</PageNumber>
          </Pagination>
          <DetailLevel>ReturnAll</DetailLevel>
        </GetSellerListRequest>";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
            request.Headers.Add("X-EBAY-API-SITEID", "0");
            request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
            request.Headers.Add("X-EBAY-API-CALL-NAME", "GetSellerList");
            request.Content = new StringContent(requestXml, Encoding.UTF8, "text/xml");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // Save raw XML for debugging
            await File.WriteAllTextAsync($"ebay_raw_response_item_page_{pageNumber}.xml", content);
            Console.WriteLine($"Saved eBay response for page {pageNumber}");

            var xmlDoc = XDocument.Parse(content);
            var ns = XNamespace.Get("urn:ebay:apis:eBLBaseComponents");

            var sellerId = xmlDoc.Descendants(ns + "Seller")
                .FirstOrDefault()?
                .Element(ns + "UserID")?
                .Value;

            var ebayItems = xmlDoc.Descendants(ns + "Item")
                .Select(x => new EbayItem
                {
                    SellerUserId = sellerId,
                    ItemId = x.Element(ns + "ItemID")?.Value,
                    Title = x.Element(ns + "Title")?.Value,
                    Price = decimal.TryParse(
                        x.Element(ns + "SellingStatus")?.Element(ns + "CurrentPrice")?.Value,
                        out var priceVal) ? priceVal : 0,
                    Currency = x.Element(ns + "SellingStatus")?.Element(ns + "CurrentPrice")?.Attribute("currencyID")?.Value,
                    PictureUrl = x.Element(ns + "PictureDetails")?.Element(ns + "PictureURL")?.Value,
                    ViewItemUrl = x.Element(ns + "ListingDetails")?.Element(ns + "ViewItemURL")?.Value,
                    StartTime = (DateTime)(DateTime.TryParse(
                        x.Element(ns + "ListingDetails")?.Element(ns + "StartTime")?.Value,
                        out var start) ? start : (DateTime?)null),
                    EndTime = (DateTime)(DateTime.TryParse(
                        x.Element(ns + "ListingDetails")?.Element(ns + "EndTime")?.Value,
                        out var end) ? end : (DateTime?)null)
                })
                .ToList();

            if (ebayItems.Any())
            {
                var itemIds = ebayItems.Select(i => i.ItemId).ToList();
                var existingItemIds = _dbContext.EbayItems
                    .Where(e => itemIds.Contains(e.ItemId))
                    .Select(e => e.ItemId)
                    .ToList();

                var newItems = ebayItems
                    .Where(i => !existingItemIds.Contains(i.ItemId))
                    .ToList();

                if (newItems.Any())
                {
                    _dbContext.EbayItems.AddRange(newItems);
                    await _dbContext.SaveChangesAsync();
                    Console.WriteLine($"Saved {newItems.Count} new items from page {pageNumber}");
                }
            }

            var hasMoreItems = xmlDoc.Descendants(ns + "HasMoreItems")
                .FirstOrDefault()?.Value;
            morePages = string.Equals(hasMoreItems, "true", StringComparison.OrdinalIgnoreCase);

            pageNumber++;
        }
    }

    // This will be triggered by Hangfire every hour
    public async Task FetchItemsForAllSellersAsync()
    {
        // Step 1: Get all eBay usernames from AspNetUsers table
        var allSellers = await _dbContext.Users
            .Where(u => !string.IsNullOrEmpty(u.EbayUsername))
            .Select(u => u.EbayUsername)
            .ToListAsync();

        if (!allSellers.Any())
        {
            Console.WriteLine("No sellers found with eBay usernames.");
            return;
        }

        Console.WriteLine($"Found {allSellers.Count} sellers to fetch items for.");

        // Step 2: Process each seller
        foreach (var ebayUserId in allSellers)
        {
            try
            {
                // Step 3: Get a valid access token (refresh if expired)
                var accessToken = await _ebayOAuthService.GetValidAccessTokenAsync(ebayUserId);

                // Step 4: Fetch seller items
                //var result = await GetSellerItemsAsync(ebayUserId, accessToken);
                await GetSellerItemsAsync(accessToken);

                Console.WriteLine($"Fetched items");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching items for {ebayUserId}: {ex.Message}");
            }
        }
    }
}