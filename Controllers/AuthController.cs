using EbayChatBot.API.DTOs;
using EbayChatBot.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using static EbayChatBot.API.DTOs.loginSignUpDto;
using EbayChatBot.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using EbayChatBot.API.Data;

namespace EbayChatBot.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _config;
        private readonly EbayChatDbContext _dbContext;
        private readonly EbayOAuthService _ebayOAuthService;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration config, EbayChatDbContext dbContext, EbayOAuthService ebayOAuthService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _config = config;
            _dbContext = dbContext;
            _ebayOAuthService = ebayOAuthService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _userManager.FindByEmailAsync(dto.Email) != null)
                return BadRequest(new { success = false, message = "Email already registered" });

            var user = new User
            {
                UserName = dto.FirstName + dto.LastName,
                Email = dto.Email,
                EbayUsername = dto.EbayUserName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });

            return Ok(new { success = true, message = "User registered successfully" });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(new { success = false, message = "Invalid credentials" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded) return Unauthorized(new { success = false, message = "Invalid credentials" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token,
                user = new { user.Id, user.Email, user.EbayUsername }
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 1. Get current user
            var ebayUsername = User.Identity?.Name;

            var seller = await _dbContext.EbayTokens
                .FirstOrDefaultAsync(x => x.EbayUserId == ebayUsername);

            if (seller != null && !string.IsNullOrEmpty(seller.AccessToken))
            {
                try
                {
                    await _ebayOAuthService.RevokeTokenAsync(seller.AccessToken);
                }
                catch (Exception ex)
                {
                    // Log error but don't block logout
                    Console.WriteLine($"Error revoking token: {ex.Message}");
                }

                // Clear token from DB / cache
                seller.AccessToken = null;
                await _dbContext.SaveChangesAsync();
            }

            // Notify background service to stop polling
            //_backgroundService.StopPollingForSeller(ebayUsername);

            return Ok("Logged out successfully.");
        }
        private string GenerateJwtToken(User user)
        {
            //var claims = new[]
            //{
            //    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            //    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            //    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            //};

            var claims = new[]
            {
               new Claim(JwtRegisteredClaimNames.Sub, user.Email),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
               new Claim(ClaimTypes.Name, user.Email) // 👈 Add this line
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
