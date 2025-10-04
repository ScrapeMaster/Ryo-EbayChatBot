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

        // Step 1: Generate eBay login URL
        public string GenerateUserAuthorizationUrl()
        {
            var clientId = _configuration["EbayOAuth:ClientId"];
            var ruName = _configuration["EbayOAuth:RuName"];
            var state = Guid.NewGuid().ToString();

            var scopes = new[]
            {
                "https://api.ebay.com/oauth/api_scope",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.readonly",
                "https://api.ebay.com/oauth/api_scope/sell.account",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.name.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.address.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.email.readonly",
                "https://api.ebay.com/oauth/api_scope/commerce.identity.phone.readonly"
            };

            var scopeString = string.Join(" ", scopes);

            return $"https://auth.ebay.com/oauth2/authorize?" +
                   $"client_id={clientId}" +
                   $"&redirect_uri={ruName}" +
                   $"&response_type=code" +
                   $"&scope={Uri.EscapeDataString(scopeString)}" +
                   $"&state={state}";
        }

        // Step 2: Exchange code for token
        public async Task<EbayTokenResponse> ExchangeCodeForAccessTokenAsync(string code)
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
                { "redirect_uri", ruName }
            });

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Token Response Content: {content}");

            response.EnsureSuccessStatusCode();

            var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

            // Save to DB
            var token = new EbayToken
            {
                AccessToken = tokenResponse.access_token,
                RefreshToken = tokenResponse.refresh_token,
                ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
                EbayUserId = "Unknown", // you can fetch user profile here
                Email = "test"
            };

            _ebayDbContext.EbayTokens.Add(token);
            await _ebayDbContext.SaveChangesAsync();

            return tokenResponse;
        }

        // Step 3: Refresh token
        public async Task<EbayTokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            var clientId = _configuration["EbayOAuth:ClientId"];
            var clientSecret = _configuration["EbayOAuth:ClientSecret"];
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "scope", "https://api.ebay.com/oauth/api_scope" }
            });

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Refresh Token Response Content: {content}");

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<EbayTokenResponse>(content);
        }
    }

    public class EbayTokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public string token_type { get; set; }
    }
}
