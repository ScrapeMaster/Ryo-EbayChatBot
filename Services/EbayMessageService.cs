using EbayChatBot.API.Models;
using EbayChatBot.API.Data;
using System.Text;
using System.Xml.Linq;


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
        var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<GetMemberMessagesRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
  <RequesterCredentials>
    <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
  </RequesterCredentials>
  <MailMessageType>All</MailMessageType>
  <DetailLevel>ReturnMessages</DetailLevel>
  <Pagination>
    <EntriesPerPage>50</EntriesPerPage>
    <PageNumber>1</PageNumber>
  </Pagination>
</GetMemberMessagesRequest>";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/ws/api.dll");
        request.Headers.Add("X-EBAY-API-CALL-NAME", "GetMemberMessages");
        request.Headers.Add("X-EBAY-API-SITEID", "0");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
        request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

        var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(content);
        var ns = XNamespace.Get("urn:ebay:apis:eBLBaseComponents");

        foreach (var msg in doc.Descendants(ns + "MemberMessageExchange"))
        {
            var question = msg.Element(ns + "Question");
            if (question == null) continue;

            var externalMessageId = question.Element(ns + "MessageID")?.Value;
            if (_dbContext.ChatMessages.Any(m => m.ExternalMessageId == externalMessageId))
                continue;

            var sender = question.Element(ns + "SenderID")?.Value;
            var text = question.Element(ns + "Body")?.Value;
            var itemId = question.Element(ns + "ItemID")?.Value;
            var date = DateTime.Parse(question.Element(ns + "CreationDate")?.Value ?? DateTime.UtcNow.ToString());

            var chat = new ChatMessage
            {
                ExternalMessageId = externalMessageId,
                SenderEbayUsername = sender,
                ReceiverId = 1, // You can determine based on itemId or business logic
                Message = text,
                Timestamp = date,
                MessageDirection = MessageDirection.Incoming
            };

            _dbContext.ChatMessages.Add(chat);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task SendMessageToEbay(string ebayAuthToken, string itemId, string buyerUserId, string messageBody)
    {
        var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<AddMemberMessageAAQToPartnerRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
  <RequesterCredentials>
    <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
  </RequesterCredentials>
  <ItemID>{itemId}</ItemID>
  <MemberMessage>
    <RecipientID>{buyerUserId}</RecipientID>
    <Body>{System.Security.SecurityElement.Escape(messageBody)}</Body>
    <EmailCopyToSender>true</EmailCopyToSender>
  </MemberMessage>
</AddMemberMessageAAQToPartnerRequest>";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.sandbox.ebay.com/ws/api.dll");
        request.Headers.Add("X-EBAY-API-CALL-NAME", "AddMemberMessageAAQToPartner");
        request.Headers.Add("X-EBAY-API-SITEID", "0");
        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
        request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to send message to eBay: " + responseBody);
        }
    }
}