# Update Dapr Configuration for Aspire Integration
# This script automatically updates Dapr component configurations with the correct ports from Aspire containers

Write-Host "Updating Dapr configuration for Aspire integration..." -ForegroundColor Green

# Get RabbitMQ port from Aspire container
$rabbitmqContainer = docker ps --filter "name=rabbitmq-" --format "{{.Names}}" | Select-Object -First 1
if ($rabbitmqContainer) {
    $rabbitmqPort = docker port $rabbitmqContainer | Where-Object { $_ -like "*5672/tcp*" } | ForEach-Object { ($_ -split " -> ")[1] } | ForEach-Object { ($_ -split ":")[1] } | Select-Object -First 1
    Write-Host "Found RabbitMQ on port: $rabbitmqPort" -ForegroundColor Yellow
    
    # Update pubsub.yaml
    $pubsubFile = "Infrastructure\dapr\components\pubsub.yaml"
    $content = Get-Content $pubsubFile -Raw
    $newConnectionString = "amqp://guest:guest@localhost:$rabbitmqPort"
    $content = $content -replace "value: `"amqp://guest:guest@localhost:\d+.*`"", "value: `"$newConnectionString`""
    Set-Content $pubsubFile -Value $content
    Write-Host "Updated pubsub.yaml with port $rabbitmqPort" -ForegroundColor Green
} else {
    Write-Warning "RabbitMQ container not found"
}

# Get Redis port from Aspire container
$redisContainer = docker ps --filter "name=redis-" --format "{{.Names}}" | Select-Object -First 1
if ($redisContainer) {
    $redisPort = docker port $redisContainer | Where-Object { $_ -like "*6379/tcp*" } | ForEach-Object { ($_ -split " -> ")[1] } | ForEach-Object { ($_ -split ":")[1] } | Select-Object -First 1
    Write-Host "Found Redis on port: $redisPort" -ForegroundColor Yellow
    
    # Update statestore.yaml if it exists
    $statestoreFile = "Infrastructure\dapr\components\statestore.yaml"
    if (Test-Path $statestoreFile) {
        $content = Get-Content $statestoreFile -Raw
        if ($content -like "*redisHost*") {
            $content = $content -replace "value: `"localhost:\d+`"", "value: `"localhost:$redisPort`""
            Set-Content $statestoreFile -Value $content
            Write-Host "Updated statestore.yaml with port $redisPort" -ForegroundColor Green
        }
    }
} else {
    Write-Warning "Redis container not found"
}

# Copy updated components to Dapr default location
$daprComponentsDir = "$env:USERPROFILE\.dapr\components"
if (!(Test-Path $daprComponentsDir)) {
    New-Item -ItemType Directory -Path $daprComponentsDir -Force
}

Copy-Item -Path "Infrastructure\dapr\components\*" -Destination $daprComponentsDir -Force
Write-Host "Copied updated components to Dapr default location" -ForegroundColor Green

Write-Host "Dapr configuration update completed!" -ForegroundColor Green
Write-Host "You can now restart your Aspire application." -ForegroundColor Cyan 