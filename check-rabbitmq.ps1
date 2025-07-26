# Check RabbitMQ Status and Queues
# This script checks RabbitMQ status and shows event queues

Write-Host "=== Checking RabbitMQ Status ===" -ForegroundColor Green

# Check if RabbitMQ container is running
try {
    $rabbitContainer = docker ps --filter "name=rabbitmq-microservices" --format "table {{.Names}}\t{{.Status}}"
    if ($rabbitContainer -match "rabbitmq-microservices") {
        Write-Host "✓ RabbitMQ container is running" -ForegroundColor Green
    } else {
        Write-Host "✗ RabbitMQ container is not running" -ForegroundColor Red
        Write-Host "Run start-with-events.ps1 to start RabbitMQ" -ForegroundColor Yellow
        exit 1
    }
} catch {
    Write-Host "Error checking RabbitMQ container: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# RabbitMQ Management API endpoints
$rabbitMQApi = "http://localhost:15672/api"
$credentials = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("guest:guest"))
$headers = @{
    "Authorization" = "Basic $credentials"
    "Content-Type" = "application/json"
}

Write-Host "`n=== Checking RabbitMQ Exchanges ===" -ForegroundColor Yellow

try {
    $exchanges = Invoke-RestMethod -Uri "$rabbitMQApi/exchanges" -Headers $headers
    $microserviceExchange = $exchanges | Where-Object { $_.name -eq "microservice.events" }
    
    if ($microserviceExchange) {
        Write-Host "✓ microservice.events exchange exists" -ForegroundColor Green
        Write-Host "  Type: $($microserviceExchange.type)" -ForegroundColor Cyan
        Write-Host "  Durable: $($microserviceExchange.durable)" -ForegroundColor Cyan
    } else {
        Write-Host "✗ microservice.events exchange not found" -ForegroundColor Red
        Write-Host "  This will be created when first message is published" -ForegroundColor Yellow
    }
} catch {
    Write-Host "Error checking exchanges: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Checking RabbitMQ Queues ===" -ForegroundColor Yellow

try {
    $queues = Invoke-RestMethod -Uri "$rabbitMQApi/queues" -Headers $headers
    
    $expectedQueues = @(
        "order.created",
        "order.completed", 
        "order.cancelled",
        "order.status-changed",
        "user.registered",
        "product.stock-updated"
    )
    
    foreach ($expectedQueue in $expectedQueues) {
        $queue = $queues | Where-Object { $_.name -eq $expectedQueue }
        if ($queue) {
            Write-Host "✓ Queue '$expectedQueue' exists" -ForegroundColor Green
            Write-Host "  Messages: $($queue.messages)" -ForegroundColor Cyan
            Write-Host "  Consumers: $($queue.consumers)" -ForegroundColor Cyan
        } else {
            Write-Host "○ Queue '$expectedQueue' not yet created" -ForegroundColor Yellow
            Write-Host "  Will be created when consumer starts" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "Error checking queues: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Checking RabbitMQ Bindings ===" -ForegroundColor Yellow

try {
    $bindings = Invoke-RestMethod -Uri "$rabbitMQApi/bindings" -Headers $headers
    $eventBindings = $bindings | Where-Object { $_.source -eq "microservice.events" }
    
    if ($eventBindings.Count -gt 0) {
        Write-Host "✓ Found $($eventBindings.Count) bindings for microservice.events" -ForegroundColor Green
        foreach ($binding in $eventBindings) {
            Write-Host "  $($binding.source) -> $($binding.destination) [$($binding.routing_key)]" -ForegroundColor Cyan
        }
    } else {
        Write-Host "○ No bindings found for microservice.events" -ForegroundColor Yellow
        Write-Host "  Bindings will be created when consumers start" -ForegroundColor Gray
    }
} catch {
    Write-Host "Error checking bindings: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== RabbitMQ Connection Test ===" -ForegroundColor Yellow

# Test connection using .NET RabbitMQ client
$testScript = @"
using System;
using RabbitMQ.Client;

try {
    var factory = new ConnectionFactory() { 
        HostName = "localhost",
        UserName = "guest",
        Password = "guest"
    };
    
    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();
    
    Console.WriteLine("✓ RabbitMQ connection test successful");
} catch (Exception ex) {
    Console.WriteLine($"✗ RabbitMQ connection test failed: {ex.Message}");
}
"@

# Save and run test script
$testScript | Out-File -FilePath "temp_rabbit_test.cs" -Encoding UTF8

try {
    # This would require RabbitMQ.Client package, so we'll skip for now
    Write-Host "○ Connection test skipped (requires RabbitMQ.Client package)" -ForegroundColor Yellow
} catch {
    Write-Host "Connection test error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if (Test-Path "temp_rabbit_test.cs") {
        Remove-Item "temp_rabbit_test.cs"
    }
}

Write-Host "`n=== RabbitMQ Management UI ===" -ForegroundColor Green
Write-Host "Access RabbitMQ Management at: http://localhost:15672" -ForegroundColor Cyan
Write-Host "Username: guest" -ForegroundColor Cyan
Write-Host "Password: guest" -ForegroundColor Cyan

Write-Host "`n=== Next Steps ===" -ForegroundColor Green
Write-Host "1. Start all microservices using start-with-events.ps1" -ForegroundColor Yellow
Write-Host "2. Run test-events.ps1 to test event publishing" -ForegroundColor Yellow
Write-Host "3. Check service logs to verify event processing" -ForegroundColor Yellow
Write-Host "4. Monitor queues in RabbitMQ Management UI" -ForegroundColor Yellow