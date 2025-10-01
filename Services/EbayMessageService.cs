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

    public async Task SyncMessagesAsync(string ebayUserID)
    {
        string ebayAuthToken = "";
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


                // Logic for Placed Orders only
                //var orderItem = await _dbContext.OrderItems
                //    .Include(oi => oi.Order)
                //    .FirstOrDefaultAsync(oi => oi.EbayItemId == itemIdStr);

                //if (orderItem?.Order == null)
                //{
                //    Console.WriteLine($"No order found for ItemID: {itemIdStr}");
                //    continue;
                //}

                //var sellerId = orderItem.Order.SellerId;

                //var chat = new ChatMessage
                //{
                //    ExternalMessageId = externalMessageId,
                //    ItemId = itemIdStr,
                //    SenderEbayUsername = senderID,
                //    SenderType = SenderType.Buyer,
                //    SenderEntityId = orderItem.Order.BuyerId,
                //    ReceiverType = SenderType.User,
                //    ReceiverEntityId = sellerId,
                //    Message = text,
                //    Timestamp = date,
                //    MessageDirection = MessageDirection.Incoming
                //};

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


    public async Task SendMessageToEbay(string ebayAuthToken, string itemId, string buyerUserId, string messageBody, string externalMessageId)
    {
        string ebayAuthToken2 = "v^1.1#i^1#p^3#I^3#r^0#f^0#t^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";
        var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <AddMemberMessageRTQRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
          <RequesterCredentials>
            <eBayAuthToken>{ebayAuthToken2}</eBayAuthToken>
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
            ItemId = itemId
        };

        _dbContext.ChatMessages.Add(chat);
        await _dbContext.SaveChangesAsync();
    }

    public async Task SendAutomateMessageAsync(string orderId, string itemId, string buyerUserId, string message)
    {
        string ebayAuthToken = "";
        var xmlRequest = $@"
        <?xml version=""1.0"" encoding=""utf-8""?>
        <AddMemberMessageAAQToPartnerRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
          <RequesterCredentials>
            <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
          </RequesterCredentials>
          <ItemID>{itemId}</ItemID>
          <MemberMessage>
            <Subject>Message about your order {orderId}</Subject>
            <Body>{message}</Body>
            <QuestionType>General</QuestionType>
            <RecipientID>{buyerUserId}</RecipientID>
          </MemberMessage>
        </AddMemberMessageAAQToPartnerRequest>";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
        request.Headers.Add("X-EBAY-API-CALL-NAME", "AddMemberMessageAAQToPartner");
        request.Headers.Add("X-EBAY-API-SITEID", "0");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
        request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();
        await File.WriteAllTextAsync($"ebay_automate_response_send_message.xml", responseBody);
        Console.WriteLine($"Saved eBay response Send Message");
    }
}