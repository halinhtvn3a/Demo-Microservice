using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Refit;
using System.Reflection;
using System.Text;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Mappings;
using OrderService.Services;
using Shared.Messaging;
using Hangfire;
using Hangfire.SqlServer;
using OrderService.Clients;
using Shared.Auth;
using Shared.Events;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for HTTP/2 support
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

// Add ServiceDefaults (includes OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

//// Add Entity Framework with SQL Server
//builder.Services.AddDbContext<OrderDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("OrderDb")));
builder.AddSqlServerDbContext<OrderDbContext>("OrderDb");

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Add Controllers
builder.Services.AddControllers();

// Add Hangfire for background jobs (disabled for testing)
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("OrderDb")));

builder.Services.AddHangfireServer();

// Add Redis and HybridCache (Aspire will configure connection)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
});

builder.Services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(3)
    };
});

// Add Refit clients for external services
// Use fixed ports when not running with Aspire, service discovery when with Aspire
var userServiceUrl = builder.Configuration["ExternalServices:UserService"] ?? "http://localhost:8080";
var productServiceUrl = builder.Configuration["ExternalServices:ProductService"] ?? "http://localhost:8081";

builder.Services.AddRefitClient<IUserServiceClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(userServiceUrl));

builder.Services.AddRefitClient<IProductServiceClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(productServiceUrl));

// Add gRPC client for ProductService (HTTP/2.0 over HTTPS)
builder.Services.AddGrpcClient<OrderService.Protos.ProductGrpcService.ProductGrpcServiceClient>(options =>
{
    var grpcUrl = productServiceUrl.Replace("http://", "https://");
    options.Address = new Uri(grpcUrl);
})
.ConfigureChannel(options =>
{
    options.UnsafeUseInsecureChannelCallCredentials = true; // For development only
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        // Skip SSL validation in development
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
})
.ConfigureHttpClient(client =>
{
    // Use HTTP/2.0 for gRPC
    client.DefaultRequestVersion = new Version(2, 0);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
});

builder.Services.AddScoped<IProductGrpcClient, ProductGrpcClient>();

// Add JWT Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Service API",
        Version = "v1",
        Description = "Microservice for order management with Dapr Workflows"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add RabbitMQ Messaging
builder.Services.AddRabbitMQMessaging();

// Add Application Services
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();
builder.Services.AddScoped<IOrderGrpcService, OrderGrpcService>();
builder.Services.AddScoped<IUserValidationService, UserValidationService>();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

// Initialize database
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        context.Database.EnsureCreated();
        app.Logger.LogInformation("Database initialized successfully");
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to initialize database");
    // Continue without database for now
}

app.UseAuthentication();
app.UseAuthorization();

// Removed Dapr middleware - using native .NET with Aspire

app.MapControllers();

// Add simple home page
if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => "Order Service - REST API available at /swagger");
}

app.Run();