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
using OrderService.Workflows;
using OrderService.Workflows.Activities;
using Dapr.Workflow;
using Microsoft.Extensions.Caching.StackExchangeRedis;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (includes OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add Entity Framework with SQL Server (Aspire will configure connection)
builder.AddSqlServerDbContext<OrderDbContext>("OrderDb");

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Add Dapr
builder.Services.AddControllers().AddDapr();

// Add Dapr Workflow
builder.Services.AddDaprWorkflow(options =>
{
	options.RegisterWorkflow<OrderProcessingWorkflow>();
	options.RegisterActivity<ValidateOrderActivity>();
	options.RegisterActivity<ReserveInventoryActivity>();
	options.RegisterActivity<ProcessPaymentActivity>();
	options.RegisterActivity<UpdateOrderStatusActivity>();
	options.RegisterActivity<ReleaseInventoryActivity>();
	options.RegisterActivity<SendNotificationActivity>();
	options.RegisterActivity<ProcessShippingActivity>();
});

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

// Add Refit clients for external services (using service discovery)
builder.Services.AddRefitClient<IUserServiceClient>()
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("http://userservice"));

builder.Services.AddRefitClient<IProductServiceClient>()
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("http://productservice"));

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

// Add Application Services
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();

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
using (var scope = app.Services.CreateScope())
{
	var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
	context.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

// Add Dapr middleware
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapControllers();

// Add endpoint to display service info in development
if (app.Environment.IsDevelopment())
{
	app.MapGet("/", () => "Order Service - REST API available at /swagger, Dapr Workflows enabled");
}

app.Run();