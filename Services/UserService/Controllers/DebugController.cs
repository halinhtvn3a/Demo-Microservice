using Microsoft.AspNetCore.Mvc;
using Shared.Auth;

namespace UserService.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DebugController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("jwt-secret-hash")]
    public IActionResult GetJwtSecretHash()
    {
        var secret = JwtConfiguration.GetJwtSecret(_configuration);
        var hash = secret.GetHashCode().ToString();

        return Ok(new
        {
            secretHash = hash,
            secretLength = secret.Length,
            source = _configuration["Jwt:Secret"] != null ? "appsettings" : "default"
        });
    }

    [HttpPost("test-token")]
    public IActionResult TestToken([FromBody] string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            return Ok(new
            {
                valid = true,
                issuer = jsonToken.Issuer,
                audience = jsonToken.Audiences.FirstOrDefault(),
                expiry = jsonToken.ValidTo,
                claims = jsonToken.Claims.Select(c => new { c.Type, c.Value }).ToList()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { valid = false, error = ex.Message });
        }
    }
}