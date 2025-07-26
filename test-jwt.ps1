# Test JWT Authentication across services
Write-Host "Testing JWT Authentication..." -ForegroundColor Green

# Step 1: Check JWT secret hash in both services
Write-Host "`n1. Checking JWT secret hash in UserService..." -ForegroundColor Yellow
try {
    $userSecretResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/debug/jwt-secret-hash" -Method GET -ErrorAction Stop
    Write-Host "UserService JWT Secret Hash: $($userSecretResponse.secretHash)" -ForegroundColor Cyan
    Write-Host "UserService JWT Secret Length: $($userSecretResponse.secretLength)" -ForegroundColor Cyan
    Write-Host "UserService JWT Secret Source: $($userSecretResponse.source)" -ForegroundColor Cyan
} catch {
    Write-Host "UserService not available or error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. Checking JWT secret hash in ProductService..." -ForegroundColor Yellow
try {
    $productSecretResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/debug/jwt-secret-hash" -Method GET -ErrorAction Stop
    Write-Host "ProductService JWT Secret Hash: $($productSecretResponse.secretHash)" -ForegroundColor Cyan
    Write-Host "ProductService JWT Secret Length: $($productSecretResponse.secretLength)" -ForegroundColor Cyan
    Write-Host "ProductService JWT Secret Source: $($productSecretResponse.source)" -ForegroundColor Cyan
} catch {
    Write-Host "ProductService not available or error: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 2: Register a test user
Write-Host "`n3. Registering test user..." -ForegroundColor Yellow
$registerBody = @{
    email = "testjwt@example.com"
    password = "Test123!"
    fullName = "JWT Test User"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/users/register" -Method POST -Body $registerBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "User registered successfully" -ForegroundColor Green
} catch {
    Write-Host "Registration failed or user already exists: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Step 3: Login to get JWT token
Write-Host "`n4. Logging in to get JWT token..." -ForegroundColor Yellow
$loginBody = @{
    email = "testjwt@example.com"
    password = "Test123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/users/login" -Method POST -Body $loginBody -ContentType "application/json" -ErrorAction Stop
    $token = $loginResponse.token
    Write-Host "Login successful! Token received." -ForegroundColor Green
    Write-Host "Token (first 50 chars): $($token.Substring(0, [Math]::Min(50, $token.Length)))..." -ForegroundColor Cyan
    
    # Step 4: Test token with UserService debug endpoint
    Write-Host "`n5. Testing token with UserService..." -ForegroundColor Yellow
    try {
        $testTokenResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/debug/test-token" -Method POST -Body "`"$token`"" -ContentType "application/json" -ErrorAction Stop
        Write-Host "Token validation in UserService: $($testTokenResponse.valid)" -ForegroundColor Green
        Write-Host "Token expiry: $($testTokenResponse.expiry)" -ForegroundColor Cyan
    } catch {
        Write-Host "Token test in UserService failed: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Step 5: Test token with ProductService
    Write-Host "`n6. Testing token with ProductService..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $productBody = @{
        name = "Test Product JWT"
        description = "Testing JWT authentication"
        price = 99.99
        stock = 10
        category = "Test"
        imageUrl = "https://example.com/test.jpg"
    } | ConvertTo-Json
    
    try {
        $productResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/products" -Method POST -Body $productBody -Headers $headers -ErrorAction Stop
        Write-Host "Product created successfully with JWT token!" -ForegroundColor Green
        Write-Host "Product ID: $($productResponse.id)" -ForegroundColor Cyan
    } catch {
        Write-Host "Product creation failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error details: $errorContent" -ForegroundColor Red
        }
    }
    
} catch {
    Write-Host "Login failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nJWT Test completed!" -ForegroundColor Green