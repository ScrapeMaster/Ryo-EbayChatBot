using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using Microsoft.EntityFrameworkCore;

public class EbayOrderService
{
    private readonly HttpClient _httpClient;
    private readonly EbayChatDbContext _dbContext;

    public EbayOrderService(HttpClient httpClient, EbayChatDbContext dbContext)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
    }

    public async Task FetchAndSaveOrdersAsync(string ebayAuthToken)
    {
        try
        {
            var ns = XNamespace.Get("urn:ebay:apis:eBLBaseComponents");

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-90);

            while (true)
            {
                int pageNumber = 1;
                bool hasMoreOrders = true;

                while (hasMoreOrders)
                {
                    var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                    <GetOrdersRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
                      <RequesterCredentials>
                        <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
                      </RequesterCredentials>
                      <DetailLevel>ReturnAll</DetailLevel>
                      <OrderRole>Seller</OrderRole>
                      <OrderStatus>All</OrderStatus>
                      <CreateTimeFrom>{startDate:yyyy-MM-ddTHH:mm:ss}Z</CreateTimeFrom>
                      <CreateTimeTo>{endDate:yyyy-MM-ddTHH:mm:ss}Z</CreateTimeTo>
                      <Pagination>
                        <EntriesPerPage>100</EntriesPerPage>
                        <PageNumber>{pageNumber}</PageNumber>
                      </Pagination>
                    </GetOrdersRequest>";

                    var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
                    request.Headers.Add("X-EBAY-API-CALL-NAME", "GetOrders");
                    request.Headers.Add("X-EBAY-API-SITEID", "0");
                    request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
                    request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

                    var response = await _httpClient.SendAsync(request);
                    var xmlContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"eBay Response for {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}, Page {pageNumber}: {xmlContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"eBay API Error: {response.StatusCode}");
                        return;
                    }

                    var doc = XDocument.Parse(xmlContent);
                    var orders = doc.Descendants(ns + "Order");
                    Console.WriteLine($"Found {orders.Count()} orders in response.");

                    foreach (var o in orders)
                    {
                        var ebayOrderId = o.Element(ns + "OrderID")?.Value;
                        Console.WriteLine($"Processing OrderID: {ebayOrderId}");

                        if (string.IsNullOrEmpty(ebayOrderId))
                            continue;

                        if (await _dbContext.Orders.AnyAsync(x => x.EbayOrderId == ebayOrderId))
                            continue;

                        // Buyer
                        var buyerUserId = o.Element(ns + "BuyerUserID")?.Value;
                        var buyer = await _dbContext.Buyers.FirstOrDefaultAsync(b => b.EbayUsername == buyerUserId);
                        if (buyer == null)
                        {
                            buyer = new Buyer
                            {
                                EbayUsername = buyerUserId,
                                Email = o.Descendants(ns + "Transaction").FirstOrDefault()?.Element(ns + "Buyer")?.Element(ns + "Email")?.Value ?? "unknown@example.com",
                                Country = o.Element(ns + "ShippingAddress")?.Element(ns + "CountryName")?.Value ?? "Unknown",
                                CreatedAt = DateTime.UtcNow
                            };
                            _dbContext.Buyers.Add(buyer);
                            await _dbContext.SaveChangesAsync();
                        }

                        // Seller
                        var sellerUserId = o.Element(ns + "SellerUserID")?.Value;
                        var seller = await _dbContext.Users.FirstOrDefaultAsync(u => u.EbayUsername == sellerUserId);
                        if (seller == null)
                        {
                            seller = new User
                            {
                                UserName = sellerUserId,
                                EbayUsername = sellerUserId,
                                Email = "seller_" + sellerUserId + "@example.com",
                                PasswordHash = "default_hashed_password",
                                CreatedAt = DateTime.UtcNow
                            };
                            _dbContext.Users.Add(seller);
                            await _dbContext.SaveChangesAsync();
                        }

                        var order = new Order
                        {
                            EbayOrderId = ebayOrderId,
                            OrderDate = DateTime.TryParse(o.Element(ns + "CreatedTime")?.Value, out var date) ? date : DateTime.UtcNow,
                            Status = o.Element(ns + "OrderStatus")?.Value ?? "Unknown",
                            TotalAmount = decimal.TryParse(o.Element(ns + "Total")?.Value, out var total) ? total : 0,
                            SubtotalAmount = decimal.TryParse(o.Element(ns + "Subtotal")?.Value, out var subtotal) ? subtotal : 0,
                            ShippingCost = decimal.TryParse(o.Element(ns + "ShippingServiceSelected")?.Element(ns + "ShippingServiceCost")?.Value, out var shipping) ? shipping : 0,
                            PaymentStatus = o.Element(ns + "CheckoutStatus")?.Element(ns + "Status")?.Value ?? "Unknown",
                            PaymentMethod = o.Element(ns + "CheckoutStatus")?.Element(ns + "PaymentMethod")?.Value,
                            BuyerUserId = buyerUserId,
                            SellerUserId = sellerUserId,
                            BuyerId = buyer.BuyerId,
                            SellerId = seller.Id,
                            OrderItems = new List<OrderItem>()
                        };

                        var transactions = o.Descendants(ns + "Transaction");
                        foreach (var t in transactions)
                        {
                            var item = t.Element(ns + "Item");
                            var orderItem = new OrderItem
                            {
                                EbayItemId = item?.Element(ns + "ItemID")?.Value,
                                Title = item?.Element(ns + "Title")?.Value,
                                Quantity = int.TryParse(t.Element(ns + "QuantityPurchased")?.Value, out var qty) ? qty : 1,
                                Price = decimal.TryParse(t.Element(ns + "TransactionPrice")?.Value, out var price) ? price : 0,
                                Site = item?.Element(ns + "Site")?.Value
                            };
                            order.OrderItems.Add(orderItem);
                        }

                        _dbContext.Orders.Add(order);
                    }

                    await _dbContext.SaveChangesAsync();

                    // Paging check
                    var hasMoreStr = doc.Root?.Element(ns + "HasMoreOrders")?.Value;
                    hasMoreOrders = hasMoreStr?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

                    pageNumber++;
                }

                // Move the 90-day window backwards
                endDate = startDate;
                startDate = endDate.AddDays(-90);

                if (startDate < DateTime.UtcNow.AddYears(-2))
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FetchAndSaveOrdersAsync Error: {ex.Message}\n{ex.InnerException?.Message}");
            throw;
        }
    }



    // Original
    //public async Task FetchAndSaveOrdersAsync(string ebayAuthToken)
    //{
    //    try
    //    {
    //        var xmlRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
    //<GetOrdersRequest xmlns=""urn:ebay:apis:eBLBaseComponents"">
    //  <RequesterCredentials>
    //    <eBayAuthToken>{ebayAuthToken}</eBayAuthToken>
    //  </RequesterCredentials>
    //  <DetailLevel>ReturnAll</DetailLevel>
    //  <OrderRole>Seller</OrderRole>
    //  <OrderStatus>All</OrderStatus>
    //  <CreateTimeFrom>2025-01-01T00:00:00.000Z</CreateTimeFrom>
    //  <CreateTimeTo>2025-05-01T23:59:59.000Z</CreateTimeTo>
    //  <Pagination>
    //    <EntriesPerPage>100</EntriesPerPage>
    //    <PageNumber>1</PageNumber>
    //  </Pagination>
    //</GetOrdersRequest>";

    //        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/ws/api.dll");
    //        request.Headers.Add("X-EBAY-API-CALL-NAME", "GetOrders");
    //        request.Headers.Add("X-EBAY-API-SITEID", "0");
    //        request.Headers.Add("X-EBAY-API-COMPATIBILITY-LEVEL", "967");
    //        request.Content = new StringContent(xmlRequest, Encoding.UTF8, "text/xml");

    //        var response = await _httpClient.SendAsync(request);
    //        var xmlContent = await response.Content.ReadAsStringAsync();
    //        Console.WriteLine($"eBay Response: {xmlContent}");

    //        if (!response.IsSuccessStatusCode)
    //        {
    //            Console.WriteLine($"eBay API Error: {response.StatusCode}");
    //            return;
    //        }

    //        var doc = XDocument.Parse(xmlContent);
    //        var ns = XNamespace.Get("urn:ebay:apis:eBLBaseComponents");

    //        var orders = doc.Descendants(ns + "Order");
    //        Console.WriteLine($"Found {orders.Count()} orders in response.");

    //        foreach (var o in orders)
    //        {
    //            var ebayOrderId = o.Element(ns + "OrderID")?.Value;
    //            Console.WriteLine($"Processing OrderID: {ebayOrderId}");

    //            if (string.IsNullOrEmpty(ebayOrderId))
    //            {
    //                Console.WriteLine("Skipping order with missing OrderID.");
    //                continue;
    //            }

    //            if (await _dbContext.Orders.AnyAsync(x => x.EbayOrderId == ebayOrderId))
    //            {
    //                Console.WriteLine($"Order {ebayOrderId} already exists, skipping.");
    //                continue;
    //            }

    //            // Map Buyer
    //            var buyerUserId = o.Element(ns + "BuyerUserID")?.Value;
    //            var buyer = await _dbContext.Buyers.FirstOrDefaultAsync(b => b.EbayUsername == buyerUserId);
    //            if (buyer == null)
    //            {
    //                buyer = new Buyer
    //                {
    //                    EbayUsername = buyerUserId,
    //                    Email = o.Descendants(ns + "Transaction").FirstOrDefault()?.Element(ns + "Buyer")?.Element(ns + "Email")?.Value ?? "unknown@example.com",
    //                    Country = o.Element(ns + "ShippingAddress")?.Element(ns + "CountryName")?.Value ?? "Unknown",
    //                    CreatedAt = DateTime.UtcNow
    //                };
    //                _dbContext.Buyers.Add(buyer);
    //                await _dbContext.SaveChangesAsync();
    //                Console.WriteLine($"Created buyer: {buyerUserId}");
    //            }

    //            // Map Seller
    //            var sellerUserId = o.Element(ns + "SellerUserID")?.Value;
    //            var seller = await _dbContext.Users.FirstOrDefaultAsync(u => u.EbayUsername == sellerUserId);
    //            if (seller == null)
    //            {
    //                seller = new User
    //                {
    //                    Username = sellerUserId, // Fallback, adjust if needed
    //                    EbayUsername = sellerUserId,
    //                    Email = "seller_" + sellerUserId + "@example.com",
    //                    Password = "default_hashed_password", // Replace with proper hashing
    //                    Role = "Seller",
    //                    CreatedAt = DateTime.UtcNow,
    //                    TeamId = 1 // Adjust based on your Teams table
    //                };
    //                _dbContext.Users.Add(seller);
    //                await _dbContext.SaveChangesAsync();
    //                Console.WriteLine($"Created seller: {sellerUserId}");
    //            }

    //            var order = new Order
    //            {
    //                EbayOrderId = ebayOrderId,
    //                OrderDate = DateTime.TryParse(o.Element(ns + "CreatedTime")?.Value, out var date) ? date : DateTime.UtcNow,
    //                Status = o.Element(ns + "OrderStatus")?.Value ?? "Unknown",
    //                TotalAmount = decimal.TryParse(o.Element(ns + "Total")?.Value, out var total) ? total : 0,
    //                SubtotalAmount = decimal.TryParse(o.Element(ns + "Subtotal")?.Value, out var subtotal) ? subtotal : 0,
    //                ShippingCost = decimal.TryParse(o.Element(ns + "ShippingServiceSelected")?.Element(ns + "ShippingServiceCost")?.Value, out var shipping) ? shipping : 0,
    //                PaymentStatus = o.Element(ns + "CheckoutStatus")?.Element(ns + "Status")?.Value ?? "Unknown",
    //                PaymentMethod = o.Element(ns + "CheckoutStatus")?.Element(ns + "PaymentMethod")?.Value,
    //                BuyerUserId = buyerUserId,
    //                SellerUserId = sellerUserId,
    //                BuyerId = buyer.BuyerId,
    //                SellerId = seller.UserId,
    //                OrderItems = new List<OrderItem>()
    //            };

    //            var transactions = o.Descendants(ns + "Transaction");
    //            Console.WriteLine($"Found {transactions.Count()} transactions for OrderID: {ebayOrderId}");

    //            foreach (var t in transactions)
    //            {
    //                var item = t.Element(ns + "Item");
    //                var orderItem = new OrderItem
    //                {
    //                    EbayItemId = item?.Element(ns + "ItemID")?.Value,
    //                    Title = item?.Element(ns + "Title")?.Value,
    //                    Quantity = int.TryParse(t.Element(ns + "QuantityPurchased")?.Value, out var qty) ? qty : 1,
    //                    Price = decimal.TryParse(t.Element(ns + "TransactionPrice")?.Value, out var price) ? price : 0,
    //                    Site = item?.Element(ns + "Site")?.Value
    //                };
    //                order.OrderItems.Add(orderItem);
    //            }

    //            _dbContext.Orders.Add(order);
    //            Console.WriteLine($"Added Order to DbContext: {order.EbayOrderId}");
    //        }

    //        try
    //        {
    //            await _dbContext.SaveChangesAsync();
    //            Console.WriteLine("Successfully saved orders to database.");
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine($"Database Save Error: {ex.Message}\n{ex.InnerException?.Message}");
    //            throw;
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"FetchAndSaveOrdersAsync Error: {ex.Message}\n{ex.InnerException?.Message}");
    //        throw;
    //    }
    //}

    public async Task<List<Order>> GetOrdersFromDbAsync()
    {
        return await _dbContext.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Buyer)
            .Include(o => o.Seller)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}