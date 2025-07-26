# Script to update Dapr components with correct connection strings from running containers

# Get RabbitMQ port
$rabbitmqPort = docker port $(docker ps --filter "ancestor=rabbitmq:4.1-management" --format "{{.Names}}") 5672
if ($rabbitmqPort) {
    $rabbitmqHost = $rabbitmqPort.Replace("127.0.0.1:", "localhost:")
    $rabbitmqConnection = "amqp://guest:guest@$rabbitmqHost"
    Write-Host "RabbitMQ Connection: $rabbitmqConnection"
} else {
    Write-Error "RabbitMQ container not found"
    exit 1
}

# Get Redis port from Aspire container
$redisContainer = docker ps --filter "name=redis-" --format "{{.Names}}"
if ($redisContainer) {
    $redisPort = docker port $redisContainer 6379
    $redisHost = $redisPort.Replace("127.0.0.1:", "localhost:")
    Write-Host "Redis Host: $redisHost"
} else {
    Write-Error "Redis container not found"
    exit 1
}

# Create components directory
$componentsDir = "Infrastructure/dapr/components-temp"
New-Item -ItemType Directory -Force -Path $componentsDir

# Create pubsub component
$pubsubContent = @"
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: connectionString
    value: "$rabbitmqConnection"
  - name: durable
    value: "true"
  - name: deletedWhenUnused
    value: "false"
  - name: autoAck
    value: "false"
  - name: reconnectWait
    value: "0"
  - name: concurrency
    value: "10"
"@

$pubsubContent | Out-File -FilePath "$componentsDir/pubsub.yaml" -Encoding UTF8

# Create statestore component
$statestoreContent = @"
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: "$redisHost"
  - name: redisPassword
    value: ""
  - name: actorStateStore
    value: "true"
"@

$statestoreContent | Out-File -FilePath "$componentsDir/statestore.yaml" -Encoding UTF8

# Create secrets component
$secretsContent = @"
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: secrets
spec:
  type: secretstores.local.env
  version: v1
  metadata: []
"@

$secretsContent | Out-File -FilePath "$componentsDir/secrets.yaml" -Encoding UTF8

Write-Host "Dapr components updated successfully!"
Write-Host "Components directory: $componentsDir"