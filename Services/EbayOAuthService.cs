using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using EbayChatBot.API.Data;
using EbayChatBot.API.Models;
using Microsoft.EntityFrameworkCore;
#nullable disable

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

        public async Task<string> GetValidAccessTokenAsync(string ebayUserId)
        {
            var token = await _ebayDbContext.EbayTokens.FirstOrDefaultAsync(t => t.EbayUserId == ebayUserId);
            if (token == null) throw new Exception("User not connected to eBay");

            if (token.ExpiresAt > DateTime.UtcNow)
                return token.AccessToken;

            // Refresh token
            var clientId = _configuration["EbayOAuth:ClientId"];
            var clientSecret = _configuration["EbayOAuth:ClientSecret"];
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", token.RefreshToken },
                { "scope", "https://api.ebay.com/oauth/api_scope" }
            });
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

            // update DB
            token.AccessToken = tokenResponse.access_token;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in);
            await _ebayDbContext.SaveChangesAsync();

            return token.AccessToken;
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
                Console.WriteLine("Getting User Profile");

                var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://apiz.ebay.com/commerce/identity/v1/user/");

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await _httpClient.SendAsync(request);

                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Raw Response: " + content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"eBay API Error: {response.StatusCode} - {content}");
                }

                var userProfile = JsonSerializer.Deserialize<EbayUserProfile>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                await File.WriteAllTextAsync("ebay_user_profile.json", content);

                return userProfile;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to fetch eBay user profile", ex);
            }
        }


        //    public async Task<string> ExchangeCodeForAccessTokenAsync(string code)
        //    {
        //        var clientId = _configuration["EbayOAuth:ClientId"];
        //        var clientSecret = _configuration["EbayOAuth:ClientSecret"];
        //        var ruName = _configuration["EbayOAuth:RuName"];

        //        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        //        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
        //        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        //        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        //                                {
        //                                    { "grant_type", "authorization_code" },
        //                                    { "code", code },
        //                                    { "redirect_uri", ruName }
        //                                });

        //        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        //        var response = await _httpClient.SendAsync(request);
        //        _logger.LogInformation($"Token Response: {JsonSerializer.Serialize(response)}");
        //        response.EnsureSuccessStatusCode();

        //        var content = await response.Content.ReadAsStringAsync();
        //        var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

        //        // Get user info
        //        var userProfile = await GetUserProfileAsync(tokenResponse.access_token);

        //        Console.WriteLine($"Passed User Profile");
        //        // Save to DB
        //        var token = new EbayToken
        //        {
        //            AccessToken = tokenResponse.access_token,
        //            RefreshToken = tokenResponse.refresh_token,
        //            ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
        //            EbayUserId = userProfile?.userId,
        //            Email = "test"
        //        };

        //        _ebayDbContext.EbayTokens.Add(token);
        //        await _ebayDbContext.SaveChangesAsync();

        //        return tokenResponse.access_token;
        //    }

        //}

        public async Task<string> ExchangeCodeForAccessTokenAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Code cannot be null or empty", nameof(code));

            var clientId = _configuration["EbayOAuth:ClientId"];
            var clientSecret = _configuration["EbayOAuth:ClientSecret"];
            var ruName = _configuration["EbayOAuth:RuName"];

            // Validate configuration
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(ruName))
                throw new InvalidOperationException("Ebay OAuth configuration is missing");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.ebay.com/identity/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", ruName }
            });

            try
            {
                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Ebay token exchange failed: {response.StatusCode} - {errorContent}");
                    response.EnsureSuccessStatusCode();
                }

                var content = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<EbayTokenResponse>(content);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.access_token))
                    throw new InvalidOperationException("Invalid token response from Ebay");

                // Get user info
                var userProfile = await GetUserProfileAsync(tokenResponse.access_token);

                // Save to DB
                var token = new EbayToken
                {
                    AccessToken = tokenResponse.access_token,
                    RefreshToken = tokenResponse.refresh_token,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.expires_in),
                    EbayUserId = userProfile?.username,
                    Email = userProfile?.businessAccount?.email,
                };

                _ebayDbContext.EbayTokens.Add(token);
                await _ebayDbContext.SaveChangesAsync();

                return tokenResponse.access_token;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed during Ebay token exchange");
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Failed to save token to database");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Ebay token exchange");
                throw;
            }
        }
    }
        public class EbayTokenResponse
        {
            public string access_token { get; set; }
            public string refresh_token { get; set; }
            public int expires_in { get; set; }
        }
    }
