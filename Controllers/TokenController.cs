using Intellimix_Template.Models;
using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;

namespace Intellimix_Template.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILiteDatabase _liteDb;
        private readonly ILogger<TokenController> _logger;
        public TokenController(IConfiguration configuration,ILiteDatabase database,ILogger<TokenController> logger )
        {
            _configuration= configuration;
            _liteDb = database;
            _logger = logger;
        }
        [EnableRateLimiting("fixed")]
        [HttpPost("login")]
        [Consumes("application/json", "application/x-www-form-urlencoded")]
        public IActionResult Login([FromBody] Models.LoginRequst request)
        {
            // TODO: Validate credentials (here just accept demo user)
            try
            {
                var validate = new LoginValidator();
                var results = validate.Validate(request);
                if (!results.IsValid) {
                    return Unauthorized();
                }
                //usual method
                //if (request.username != "test" || request.password != "pass")
                //    return Unauthorized();

                var tokens = GenerateTokens(request.username, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(tokens);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Login Failed");
                return StatusCode(500, "An error occurred during login");
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public IActionResult Refresh([FromBody] Models.RefreshRequest request)
        {
            try
            {
                var col = _liteDb.GetCollection<RefreshToken>("refreshTokens");
                var stored = col.FindOne(x => x.Id == HashToken(request.Refreshtoken) && !x.Revoked);

                if (stored == null || stored.ExpiresAtUtc < DateTimeOffset.UtcNow)
                    return Unauthorized();

                // Optionally revoke old token to prevent reuse
                stored.Revoked = true;
                col.Update(stored);

                var tokens = GenerateTokens(stored.UserId, HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh failed");
                return StatusCode(500, "An error occurred while refreshing the token");
            }
        }

        [Authorize]
        [HttpPost("revoke")]
        public IActionResult Revoke([FromBody] Models.RefreshRequest request)
        {
            try
            {
                var col = _liteDb.GetCollection<RefreshToken>("refreshTokens");
                var stored = col.FindOne(x => x.Id == HashToken(request.Refreshtoken));
                if (stored == null) return NotFound();

                stored.Revoked = true;
                col.Update(stored);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Revoke failed");
                return StatusCode(500, "An error occurred while revoking the token");
            }
        }

        private TokenResponse GenerateTokens(string userId, string ipAddress)
        {
            try
            {

                // Access token
                var key = new SymmetricSecurityKey(Convert.FromBase64String(_configuration["Jwt:SigningKeyBase64"]));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);



                var descriptor = new SecurityTokenDescriptor
                {
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Audience"],
                    Claims = new[]
                            {
                            new Claim(JwtRegisteredClaimNames.Sub, "user123"),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        }.ToDictionary(c => c.Type, c => (object)c.Value),
                    Expires = DateTime.UtcNow.AddMinutes(15),
                    SigningCredentials = creds
                };

                var accessToken = new JsonWebTokenHandler().CreateToken(descriptor);

                // Refresh token
                var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                var refresh = new RefreshToken
                {
                    Id = HashToken(refreshToken),
                    UserId = userId,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    CreatedByIp = ipAddress,
                    Revoked = false
                };

                var col = _liteDb.GetCollection<RefreshToken>("refreshTokens");
                col.Insert(refresh);

                return new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 900 // seconds
                };
            }
            catch (Exception )
            {

                throw;
            }
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(token)));
        }

    }
}
