using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EbayChatBot.API.Services;
using EbayChatBot.API.Data;

namespace EbayChatBot.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EbayAuthController : ControllerBase
    {
        private readonly EbayOAuthService _ebayOAuthService;
        private readonly EbayChatDbContext _ebayDbContext;

        public EbayAuthController(EbayOAuthService ebayOAuthService,
            EbayChatDbContext ebayDbContext)
        {
            _ebayOAuthService = ebayOAuthService;
            _ebayDbContext = ebayDbContext;
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

<<<<<<< HEAD
            return Redirect("http://localhost:8080/"); // redirect to frontend dashboard
        }


        [HttpGet("check-connection")]
        public async Task<IActionResult> CheckConnection()
        {
            // Replace with actual user identification logic
            var userId = "f1ambe_158";

            var token = await _ebayDbContext.EbayTokens.FirstOrDefaultAsync(t => t.EbayUserId == userId);
            return Ok(new { connected = token != null });
=======
            return Ok(tokenResponse);
        }

        // Step 3 (optional): Refresh token
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var newToken = await _ebayOAuthService.RefreshAccessTokenAsync(refreshToken);
            return Ok(newToken);
>>>>>>> Connect_To_Ebay_Plus_Identity
        }
    }
}
