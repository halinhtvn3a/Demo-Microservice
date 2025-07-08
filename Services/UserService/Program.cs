using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using UserService.Data;
using UserService.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (includes OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddControllers().AddDapr();

// Add Entity Framework with SQL Server (Aspire will configure connection)
builder.AddSqlServerDbContext<UserDbContext>("UserDb");

// Add Redis and HybridCache (Connection will be configured by Aspire)
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
});

builder.Services.AddHybridCache(options =>
{
	options.DefaultEntryOptions = new()
	{
		Expiration = TimeSpan.FromMinutes(5),
		LocalCacheExpiration = TimeSpan.FromMinutes(1)
	};
});

// Add JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "key-default-of-the-microservice-demo-(hope-it-works)";
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

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "User Service API",
		Version = "v1",
		Description = "Microservice for user authentication and management"
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
builder.Services.AddScoped<IUserService, UserServiceImpl>();

var app = builder.Build();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI(c =>
	{
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API V1");
		c.RoutePrefix = string.Empty; // Serve Swagger UI at root
	});
}

// Initialize database
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<UserDbContext>();
	context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// Add Dapr middleware
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapControllers();

app.Run();