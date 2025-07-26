# Create a test JWT token with the shared secret
Add-Type -AssemblyName System.IdentityModel

$secret = "microservice-demo-jwt-secret-key-must-be-at-least-32-characters-long-for-security"
$key = [System.Text.Encoding]::ASCII.GetBytes($secret)

# Create JWT payload
$payload = @{
    sub = "1"
    email = "test@example.com"
    name = "Test User"
    iat = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    exp = [DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
} | ConvertTo-Json -Compress

# Create JWT header
$header = @{
    alg = "HS256"
    typ = "JWT"
} | ConvertTo-Json -Compress

# Base64 encode
$encodedHeader = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
$encodedPayload = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Create signature
$message = "$encodedHeader.$encodedPayload"
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = $key
$signature = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($message))
$encodedSignature = [Convert]::ToBase64String($signature).TrimEnd('=').Replace('+', '-').Replace('/', '_')

# Create final JWT
$jwt = "$encodedHeader.$encodedPayload.$encodedSignature"

Write-Host "Generated JWT Token:" -ForegroundColor Green
Write-Host $jwt -ForegroundColor Yellow
Write-Host ""
Write-Host "Secret used: $secret" -ForegroundColor Cyan
Write-Host "Secret length: $($secret.Length)" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can use this token to test authentication in your services." -ForegroundColor Green