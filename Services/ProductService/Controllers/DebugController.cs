using Microsoft.AspNetCore.Mvc;
using Shared.Auth;

namespace ProductService.Controllers;

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
}