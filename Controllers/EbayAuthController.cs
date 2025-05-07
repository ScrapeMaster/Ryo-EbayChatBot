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

        [HttpGet("login")]
        public IActionResult Login()
        {
            var authUrl = _ebayOAuthService.GenerateUserAuthorizationUrl();
            return Redirect(authUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Authorization code is missing.");

            var tokenResponse = await _ebayOAuthService.ExchangeCodeForAccessTokenAsync(code);

            // Optionally: save tokenResponse.access_token in DB or session
            return Ok(tokenResponse);
        }

    }
}
