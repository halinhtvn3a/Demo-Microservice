# Microservice Demo Test Script
# This script tests the complete microservice demo

Write-Host "=== Microservice Demo Test Script ===" -ForegroundColor Green

# Configuration
$UserServiceUrl = "http://localhost:8080"
$ProductServiceUrl = "http://localhost:8081"
$OrderServiceUrl = "http://localhost:8082"
$NotificationServiceUrl = "http://localhost:8083"

# Test functions
function Test-ServiceHealth {
    param($ServiceName, $Url)
    
    Write-Host "Testing $ServiceName health..." -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/*/health" -Method Get -TimeoutSec 10
        Write-Host "✓ $ServiceName is healthy" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "✗ $ServiceName is unhealthy: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Test-Authentication {
    Write-Host "Testing Authentication..." -ForegroundColor Yellow
    
    # Test login
    $loginData = @{
        username = "admin"
        password = "admin123"
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$UserServiceUrl/api/auth/login" -Method Post -Body $loginData -ContentType "application/json"
        Write-Host "✓ Login successful" -ForegroundColor Green
        return $response.token
    }
    catch {
        Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Test-ProductOperations {
    param($Token)
    
    Write-Host "Testing Product Operations..." -ForegroundColor Yellow
    
    # Get products
    try {
        $products = Invoke-RestMethod -Uri "$ProductServiceUrl/api/products" -Method Get
        Write-Host "✓ Retrieved $($products.Count) products" -ForegroundColor Green
        
        if ($products.Count -gt 0) {
            $firstProduct = $products[0]
            Write-Host "  - Sample product: $($firstProduct.name) ($($firstProduct.price))" -ForegroundColor Cyan
            
            # Test stock check
            $stockResponse = Invoke-RestMethod -Uri "$ProductServiceUrl/api/products/$($firstProduct.id)/stock/check?quantity=1" -Method Get
            Write-Host "✓ Stock check: $($stockResponse.message)" -ForegroundColor Green
        }
        
        return $true
    }
    catch {
        Write-Host "✗ Product operations failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Test-OrderCreation {
    param($Token)
    
    Write-Host "Testing Order Creation..." -ForegroundColor Yellow
    
    $orderData = @{
        userId = 1
        shippingAddress = "123 Demo Street, Test City"
        items = @(
            @{
                productId = 1
                quantity = 2
            }
        )
    } | ConvertTo-Json
    
    try {
        $headers = @{ Authorization = "Bearer $Token" }
        $response = Invoke-RestMethod -Uri "$OrderServiceUrl/api/orders" -Method Post -Body $orderData -ContentType "application/json" -Headers $headers
        Write-Host "✓ Order created successfully: Order #$($response.orderNumber)" -ForegroundColor Green
        return $response.id
    }
    catch {
        Write-Host "✗ Order creation failed: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

function Test-CachePerformance {
    Write-Host "Testing Cache Performance..." -ForegroundColor Yellow
    
    # Multiple requests to test cache
    $times = @()
    for ($i = 1; $i -le 5; $i++) {
        $start = Get-Date
        try {
            Invoke-RestMethod -Uri "$ProductServiceUrl/api/products/1" -Method Get | Out-Null
            $end = Get-Date
            $duration = ($end - $start).TotalMilliseconds
            $times += $duration
            Write-Host "  Request $i: $($duration)ms" -ForegroundColor Cyan
        }
        catch {
            Write-Host "  Request $i: Failed" -ForegroundColor Red
        }
    }
    
    $avgTime = ($times | Measure-Object -Average).Average
    Write-Host "✓ Average response time: $($avgTime.ToString('F2'))ms" -ForegroundColor Green
    
    if ($avgTime -lt 100) {
        Write-Host "✓ Cache is working effectively!" -ForegroundColor Green
    }
}

# Main test execution
Write-Host "Starting microservice demo tests..." -ForegroundColor Cyan
Write-Host "Make sure all services are running with: docker-compose up" -ForegroundColor Yellow
Write-Host ""

# Test service health
$healthResults = @{
    UserService = Test-ServiceHealth "User Service" $UserServiceUrl
    ProductService = Test-ServiceHealth "Product Service" $ProductServiceUrl
}

Write-Host ""

# If core services are healthy, run functional tests
if ($healthResults.UserService -and $healthResults.ProductService) {
    # Test authentication
    $token = Test-Authentication
    
    if ($token) {
        Write-Host ""
        
        # Test product operations
        Test-ProductOperations $token
        Write-Host ""
        
        # Test cache performance
        Test-CachePerformance
        Write-Host ""
        
        # Test order creation if order service is available
        if (Test-ServiceHealth "Order Service" $OrderServiceUrl) {
            $orderId = Test-OrderCreation $token
            if ($orderId) {
                Write-Host "✓ End-to-end test completed successfully!" -ForegroundColor Green
            }
        }
    }
}

Write-Host ""
Write-Host "=== Test Summary ===" -ForegroundColor Green
Write-Host "User Service: $(if($healthResults.UserService) {'✓ Healthy'} else {'✗ Unhealthy'})" -ForegroundColor $(if($healthResults.UserService) {'Green'} else {'Red'})
Write-Host "Product Service: $(if($healthResults.ProductService) {'✓ Healthy'} else {'✗ Unhealthy'})" -ForegroundColor $(if($healthResults.ProductService) {'Green'} else {'Red'})

Write-Host ""
Write-Host "Access points:" -ForegroundColor Cyan
Write-Host "- User Service Swagger: $UserServiceUrl" -ForegroundColor White
Write-Host "- Product Service Swagger: $ProductServiceUrl" -ForegroundColor White
Write-Host "- Prometheus: http://localhost:9090" -ForegroundColor White
Write-Host "- Grafana: http://localhost:3000 (admin/admin)" -ForegroundColor White
Write-Host "- RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White

Write-Host ""
Write-Host "Demo test completed!" -ForegroundColor Green 