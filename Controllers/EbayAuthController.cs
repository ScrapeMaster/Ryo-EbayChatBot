using Microsoft.AspNetCore.Mvc;
using EbayChatBot.API.Services;

namespace EbayChatBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EbayAuthController : ControllerBase
    {
        private readonly EbayOAuthService _ebayOAuthService;

        public EbayAuthController(EbayOAuthService ebayOAuthService)
        {
            _ebayOAuthService = ebayOAuthService;
        }

        // Step 1: Redirect user to eBay login
        [HttpGet("login")]
        public IActionResult Login()
        {
            var authUrl = _ebayOAuthService.GenerateUserAuthorizationUrl();
            return Redirect(authUrl);
        }

        // Step 2: eBay redirects back here with ?code=...
        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Authorization code is missing.");

            var tokenResponse = await _ebayOAuthService.ExchangeCodeForAccessTokenAsync(code);

            return Ok(tokenResponse);
        }

        // Step 3 (optional): Refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var newToken = await _ebayOAuthService.RefreshAccessTokenAsync(refreshToken);
            return Ok(newToken);
        }
    }
}
