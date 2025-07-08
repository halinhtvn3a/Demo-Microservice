using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using System.Text;
using ProductService.Data;
using ProductService.Services;
using ProductService.Mappings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Entity Framework with InMemory database
builder.Services.AddDbContext<ProductDbContext>(options =>
	options.UseInMemoryDatabase("ProductServiceDb"));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(ProductMappingProfile));

// Add Dapr
builder.Services.AddDapr(daprClientBuilder =>
{
	daprClientBuilder.UseHttpEndpoint("http://localhost:3501");
});

// Add Redis and HybridCache
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});

builder.Services.AddHybridCache(options =>
{
	options.DefaultEntryOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(15),
		LocalCacheExpiration = TimeSpan.FromMinutes(5)
	};
});

// Add JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-secret-key-here-must-be-at-least-32-characters-long";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// Add gRPC
builder.Services.AddGrpc();

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource
		.AddService("ProductService")
		.AddAttributes(new Dictionary<string, object>
		{
			["service.instance.id"] = Environment.MachineName,
			["service.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
		}))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation()
		.AddGrpcCoreInstrumentation()
		.AddSource("ProductService"))
	.WithMetrics(metrics => metrics
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddPrometheusExporter());

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Product Service API",
		Version = "v1",
		Description = "Microservice for product management with gRPC and HybridCache"
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

// Add Application Services
builder.Services.AddScoped<IProductService, ProductServiceImpl>();

// Add Health Checks
builder.Services.AddHealthChecks()
	.AddDbContextCheck<ProductDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Service API V1");
		c.RoutePrefix = string.Empty; // Serve Swagger UI at root
	});
}

// Initialize database
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
	context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// Add Dapr middleware
app.UseCloudEvents();
app.MapSubscribeHandler();

// Add Prometheus metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Map gRPC services
app.MapGrpcService<ProductGrpcServiceImpl>();

// Add gRPC-Web support for browser clients
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });

app.MapControllers();
app.MapHealthChecks("/health");

// Add endpoint to display gRPC services in development
if (app.Environment.IsDevelopment())
{
	app.MapGet("/", () => "Product Service - REST API available at /swagger, gRPC service available on this port");
}

app.Run();