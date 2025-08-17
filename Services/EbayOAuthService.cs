using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;

namespace EbayChatBot.API.Services
{
    public class EbayOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly EbayChatDbContext _ebayDbContext;
        private readonly ILogger<EbayOAuthService> _logger;

        public EbayOAuthService(HttpClient httpClient, IConfiguration configuration, EbayChatDbContext ebayDbContext, ILogger<EbayOAuthService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _ebayDbContext = ebayDbContext;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            var clientId = _configuration["EbayOAuth:ClientId"];
            var clientSecret = _configuration["EbayOAuth:ClientSecret"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "scope", "https://api.ebay.com/oauth/api_scope" }
            };

            request.Content = new FormUrlEncodedContent(body);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

            return tokenResponse.access_token;
        }


        public string GenerateUserAuthorizationUrl()
        {
            var clientId = _configuration["EbayOAuth:ClientId"];
            var ruName = _configuration["EbayOAuth:RuName"];
            var state = Guid.NewGuid().ToString();

            // Make sure these scopes include both basic and user identity read access
            var scopes = new[]
            {
                "https://api.ebay.com/oauth/api_scope",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.readonly",
                "https://api.ebay.com/oauth/api_scope/sell.account",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.name.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.address.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.email.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.phone.readonly"
            };

            //var scopeString = string.Join(" ", scopes);

            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId), "eBay ClientId is missing.");
            if (string.IsNullOrEmpty(ruName))
                throw new ArgumentNullException(nameof(ruName), "eBay RuName is missing.");

            var authorizationUrl = $"https://auth.ebay.com/oauth2/authorize?" +
                                    $"client_id={clientId}" +
                                    $"&redirect_uri={ruName}" +
                                    $"&response_type=code" +
                                    $"&scope={Uri.EscapeDataString(_configuration["EbayOAuth:Scope"])}" +
                                    $"&state={state}";

            return authorizationUrl;
        }


        public async Task<EbayUserProfile> GetUserProfileAsync(string accessToken)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    "https://apiz.ebay.com/commerce/identity/v1/user/"
                );

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", "EBAY_US");
                Console.WriteLine($"Calling URL: {request.RequestUri}");
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"eBay API Error: {response.StatusCode} - {errorContent}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var userProfile = JsonSerializer.Deserialize<EbayUserProfile>(content);

                return userProfile;
            }
            catch (Exception ex)
            {
                // Log the exception here
                throw new ApplicationException("Failed to fetch eBay user profile", ex);
            }
        }

        public async Task<string> ExchangeCodeForAccessTokenAsync(string code)
        {
            var clientId = _configuration["EbayOAuth:ClientId"];
            var clientSecret = _configuration["EbayOAuth:ClientSecret"];
            var ruName = _configuration["EbayOAuth:RuName"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
                                    {
                                        { "grant_type", "authorization_code" },
                                        { "code", code },
                                        { "redirect_uri", "Tomoya_Nisimura-TomoyaNi-modeli-awwqzro" }
                                    });

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var response = await _httpClient.SendAsync(request);
            _logger.LogInformation($"Token Response: {JsonSerializer.Serialize(response)}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

            // Get user info
            var userProfile = await GetUserProfileAsync(tokenResponse.access_token);

            // Save to DB
            var token = new EbayToken
            {
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
                EbayUserId = userProfile?.userId,
                Email = "test"
            };

            _ebayDbContext.EbayTokens.Add(token);
            await _ebayDbContext.SaveChangesAsync();

            return tokenResponse.access_token;
        }

    }

    public class EbayTokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
    }
}
