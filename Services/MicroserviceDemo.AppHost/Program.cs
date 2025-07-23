using Aspire.Hosting;
using CommunityToolkit.Aspire.Hosting.Dapr;

var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for caching and state management
var redis = builder.AddRedis("redis");

// Add RabbitMQ for message publishing - use fixed port 5672
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Add SQL Server for databases
var sql = builder.AddSqlServer("sql");

var userDb = sql.AddDatabase("UserDb");
var productDb = sql.AddDatabase("ProductDb");
var orderDb = sql.AddDatabase("OrderDb");
var notificationDb = sql.AddDatabase("NotificationDb");

// Add Dapr with components path
var dapr = builder.AddDapr();

// Add User Service
var userService = builder.AddProject<Projects.UserService>("userservice")
    .WithReference(userDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("DAPR_COMPONENTS_PATH", Path.GetFullPath("../../Infrastructure/dapr/components"))
    .WaitFor(redis)
    .WaitFor(userDb)
    .WithDaprSidecar();

// Add Product Service
var productService = builder.AddProject<Projects.ProductService>("productservice")
    .WithReference(productDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("DAPR_COMPONENTS_PATH", Path.GetFullPath("../../Infrastructure/dapr/components"))
    .WaitFor(productDb)
    .WithDaprSidecar();

// Add Order Service
var orderService = builder.AddProject<Projects.OrderService>("orderservice")
    .WithReference(orderDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(userService)
    .WithReference(productService)
    .WithEnvironment("DAPR_COMPONENTS_PATH", Path.GetFullPath("../../Infrastructure/dapr/components"))
    .WaitFor(orderDb)
    .WithDaprSidecar();

// Add Notification Service
var notificationService = builder.AddProject<Projects.NotificationService>("notificationservice")
    .WithReference(notificationDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithEnvironment("DAPR_COMPONENTS_PATH", Path.GetFullPath("../../Infrastructure/dapr/components"))
    .WaitFor(notificationDb)
    .WaitFor(rabbitmq)
    .WithDaprSidecar();

builder.Build().Run();
