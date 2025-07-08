using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Refit;
using System.Reflection;
using System.Text;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Mappings;
using OrderService.Services;
using OrderService.Workflows;
using OrderService.Workflows.Activities;

var builder = WebApplication.CreateBuilder(args);

// Note: AddControllers is called later with AddDapr
//builder.Services.AddControllers();

// Add Entity Framework with InMemory database
builder.Services.AddDbContext<OrderDbContext>(options =>
	options.UseInMemoryDatabase("OrderServiceDb"));

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(OrderMappingProfile));

// Add Dapr
builder.Services.AddDaprClient(daprClientBuilder =>
{
	daprClientBuilder.UseHttpEndpoint("http://localhost:3502");
});

// Add Dapr Workflow
builder.Services.AddDaprWorkflow(options =>
{
	options.RegisterWorkflow<OrderProcessingWorkflow>();
	options.RegisterActivity<ValidateOrderActivity>();
	options.RegisterActivity<ReserveInventoryActivity>();
	options.RegisterActivity<ProcessPaymentActivity>();
	options.RegisterActivity<UpdateOrderStatusActivity>();
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
		Expiration = TimeSpan.FromMinutes(10),
		LocalCacheExpiration = TimeSpan.FromMinutes(3)
	};
});

// Add Refit clients for external services
builder.Services.AddRefitClient<IUserServiceClient>()
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:8080"));

builder.Services.AddRefitClient<IProductServiceClient>()
	.ConfigureHttpClient(c => c.BaseAddress = new Uri("http://localhost:8081"));

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

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
	.ConfigureResource(resource => resource
		.AddService("OrderService")
		.AddAttributes(new Dictionary<string, object>
		{
			["service.instance.id"] = Environment.MachineName,
			["service.version"] = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0"
		}))
	.WithTracing(tracing => tracing
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation()
		.AddSource("OrderService"))
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

// Add Health Checks
builder.Services.AddHealthChecks()
	.AddDbContextCheck<OrderDbContext>();

var app = builder.Build();

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

// Add Prometheus metrics endpoint
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapControllers();
app.MapHealthChecks("/health");

// Add endpoint to display service info in development
if (app.Environment.IsDevelopment())
{
	app.MapGet("/", () => "Order Service - REST API available at /swagger, Dapr Workflows enabled");
}

app.Run();