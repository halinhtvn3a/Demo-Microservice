var builder = DistributedApplication.CreateBuilder(args);

// Add Redis for caching and state management
var redis = builder.AddRedis("redis");

// Add RabbitMQ for message publishing
var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin();

// Add SQL Server for databases
var sql = builder.AddSqlServer("sql");

var userDb = sql.AddDatabase("UserDb");
var productDb = sql.AddDatabase("ProductDb");
var orderDb = sql.AddDatabase("OrderDb");
var notificationDb = sql.AddDatabase("NotificationDb");

// Add User Service
var userService = builder.AddProject<Projects.UserService>("userservice")
    .WithReference(userDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(redis)
    .WaitFor(userDb)
    .WaitFor(rabbitmq);

// Add Product Service
var productService = builder.AddProject<Projects.ProductService>("productservice")
    .WithReference(productDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(productDb)
    .WaitFor(rabbitmq);

// Add Order Service
var orderService = builder.AddProject<Projects.OrderService>("orderservice")
    .WithReference(orderDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(userService)
    .WithReference(productService)
    .WaitFor(orderDb)
    .WaitFor(rabbitmq);

// Add Notification Service
var notificationService = builder.AddProject<Projects.NotificationService>("notificationservice")
    .WithReference(notificationDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(notificationDb)
    .WaitFor(rabbitmq);

builder.Build().Run();
