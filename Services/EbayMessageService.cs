using EbayChatBot.API.Models;
using EbayChatBot.API.Data;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;


public class EbayMessageService
{
    private readonly HttpClient _httpClient;
    private readonly EbayChatDbContext _dbContext;

    public EbayMessageService(HttpClient httpClient, EbayChatDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
    }

    public async Task SyncMessagesAsync(string ebayAuthToken)
    {
        int pageNumber = 1;
        bool hasMoreItems = true;
        var ns = XNamespace.Get("urn:ebay:apis:eBLBaseComponents");

        while (hasMoreItems)
        {
            Console.WriteLine($"Fetching eBay messages: Page {pageNumber}");

            var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                <GetMemberMessagesRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
                                  <RequesterCredentials>
                                    <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
                                  </RequesterCredentials>
                                  <MailMessageType>All</MailMessageType>
                                  <DetailLevel>ReturnMessages</DetailLevel>
                                  <Pagination>
                                    <EntriesPerPage>50</EntriesPerPage>
                                    <PageNumber>{pageNumber}</PageNumber>
                                  </Pagination>
                                </GetMemberMessagesRequest>";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
            request.Headers.Add("X-EBAY-API-CALL-NAME", "GetMemberMessages");
            request.Headers.Add("X-EBAY-API-SITEID", "0");
            request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
            request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            await File.WriteAllTextAsync($"ebay_raw_response_page_{pageNumber}.xml", content);
            Console.WriteLine($"Saved eBay response for page {pageNumber}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("eBay API call failed:");
                Console.WriteLine(content);
                return;
            }

            var doc = XDocument.Parse(content);
            var messages = doc.Descendants(ns + "MemberMessageExchange").ToList();
            Console.WriteLine($"Found {messages.Count} messages on page {pageNumber}.");

            foreach (var msg in messages)
            {
                var question = msg.Element(ns + "Question");
                if (question == null) continue;

                var externalMessageId = question.Element(ns + "MessageID")?.Value;
                if (string.IsNullOrEmpty(externalMessageId)) continue;

                if (await _dbContext.ChatMessages.AnyAsync(m => m.ExternalMessageId == externalMessageId))
                {
                    Console.WriteLine($"Skipping duplicate message ID: {externalMessageId}");
                    continue;
                }

                var senderID = question.Element(ns + "SenderID")?.Value;
                var buyer = await _dbContext.Buyers.FirstOrDefaultAsync(b => b.EbayUsername == senderID);
                if (buyer == null)
                {
                    buyer = new Buyer
                    {
                        EbayUsername = senderID,
                        Email = question.Element(ns + "SenderEmail")?.Value,
                        Country = "Unknown",
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Buyers.Add(buyer);
                    await _dbContext.SaveChangesAsync();
                }


                var text = question.Element(ns + "Body")?.Value;
                var itemIdStr = question.Element(ns + "ItemID")?.Value;
                var subject = question.Element(ns + "Subject")?.Value;
                var dateStr = question.Element(ns + "CreationDate")?.Value;

                // Fallback: extract ItemID from Subject if missing
                if (string.IsNullOrWhiteSpace(itemIdStr) && !string.IsNullOrWhiteSpace(subject))
                {
                    var match = Regex.Match(subject, @"#(\d+)");
                    if (match.Success)
                    {
                        itemIdStr = match.Groups[1].Value;
                        Console.WriteLine($"Extracted ItemID from subject: {itemIdStr}");
                    }
                }

                if (string.IsNullOrWhiteSpace(itemIdStr) || !long.TryParse(itemIdStr, out var itemId))
                {
                    Console.WriteLine($"Invalid or missing ItemID: '{itemIdStr}', skipping message.");
                    continue;
                }

                var date = DateTime.TryParse(dateStr, out var parsedDate) ? parsedDate : DateTime.UtcNow;

                var chat = new ChatMessage
                {
                    ExternalMessageId = externalMessageId,
                    ItemId = itemIdStr,
                    SenderEbayUsername = senderID,
                    SenderType = SenderType.Buyer,
                    SenderEntityId = 2,
                    ReceiverType = SenderType.User,
                    ReceiverEntityId = 1,
                    Message = text,
                    Timestamp = date,
                    MessageDirection = MessageDirection.Incoming
                };

                _dbContext.ChatMessages.Add(chat);
                Console.WriteLine($"Stored message from {senderID} for ItemID {itemIdStr}");
            }

            await _dbContext.SaveChangesAsync();

            // Check if more pages exist
            var hasMoreStr = doc.Root?.Element(ns + "HasMoreItems")?.Value;
            hasMoreItems = hasMoreStr?.ToLower() == "true";
            pageNumber++;
        }

        Console.WriteLine("All pages fetched. All new messages saved successfully.");
    }


    public async Task SendMessageToEbay(string ebayAuthToken, string itemId, string buyerUserId, string messageBody,string externalMessageId)
    {
        var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <AddMemberMessageRTQRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
          <RequesterCredentials>
            <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
          </RequesterCredentials>
          <MemberMessage>
            <Body>{System.Security.SecurityElement.Escape(messageBody)}</Body>
            <ItemID>{itemId}</ItemID>
            <ParentMessageID>{externalMessageId}</ParentMessageID>
            <RecipientID>{buyerUserId}</RecipientID>
            <EmailCopyToSender>true</EmailCopyToSender>
          </MemberMessage>
        </AddMemberMessageRTQRequest>";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
        request.Headers.Add("X-EBAY-API-CALL-NAME", "AddMemberMessageRTQ");
        request.Headers.Add("X-EBAY-API-SITEID", "0");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
        request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync($"ebay_raw_response_send_message.xml", responseBody);
        Console.WriteLine($"Saved eBay response Send Message");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to send message to eBay: " + responseBody);
        }

        // Lookup seller and buyer IDs
        var seller = await _dbContext.Users.FirstOrDefaultAsync(u => u.EbayUsername == "f1ambe_158");
        var buyer = await _dbContext.Buyers.FirstOrDefaultAsync(b => b.EbayUsername == buyerUserId);

        if (seller == null)
            throw new Exception("Seller not found in database.");

        if (buyer == null)
            throw new Exception("Buyer not found in database.");

        var chat = new ChatMessage
        {
            SenderType = SenderType.User,
            SenderEntityId = seller.UserId,
            ReceiverType = SenderType.Buyer,
            ReceiverEntityId = buyer.BuyerId,
            Message = messageBody,
            Timestamp = DateTime.UtcNow,
            MessageDirection = MessageDirection.Outgoing,
            SenderEbayUsername = buyerUserId,
            ExternalMessageId = externalMessageId,
            ItemId = itemId
        };

        _dbContext.ChatMessages.Add(chat);
        await _dbContext.SaveChangesAsync();
    }
}