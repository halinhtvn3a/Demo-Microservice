using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Shared.Auth;

public static class JwtConfiguration
{
    // Shared JWT secret - should be same across all services
    public const string DefaultJwtSecret = "microservice-demo-jwt-secret-key-must-be-at-least-32-characters-long-for-security";

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecret = configuration["Jwt:Secret"] ?? DefaultJwtSecret;
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        return services;
    }

    public static string GetJwtSecret(IConfiguration configuration)
    {
        return configuration["Jwt:Secret"] ?? DefaultJwtSecret;
    }
}