<#
.SYNOPSIS
    Quick authentication test script
.DESCRIPTION
    Quickly test JWT authentication with different users
#>

param(
    [string]$BaseUrl = "http://localhost:5236"
)

$ErrorActionPreference = "Stop"

function Test-Login {
    param(
        [string]$Username,
        [string]$Password
    )

    Write-Host "`nTesting: $Username / $Password" -ForegroundColor Yellow

    try {
        $response = Invoke-RestMethod `
            -Uri "$BaseUrl/api/authentication/login" `
            -Method POST `
            -Body (@{ username = $Username; password = $Password } | ConvertTo-Json) `
            -ContentType "application/json"

        if ($response.success) {
            Write-Host "✓ SUCCESS" -ForegroundColor Green
            Write-Host "  Token: $($response.token.Substring(0, 30))..." -ForegroundColor DarkGray
            Write-Host "  User: $($response.username)" -ForegroundColor Cyan
            Write-Host "  Message: $($response.message)" -ForegroundColor Gray
            return $true
        } else {
            Write-Host "✗ FAILED: $($response.message)" -ForegroundColor Red
            return $false
        }
    }
    catch {
        Write-Host "✗ ERROR: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  JWT Authentication Quick Test" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor White
Write-Host ""

# Test all available users
$users = @(
    @{ Username = "admin"; Password = "password123" }
    @{ Username = "user1"; Password = "pass123" }
    @{ Username = "testuser"; Password = "testpassword" }
    @{ Username = "demo"; Password = "demo" }
)

$results = @()
foreach ($user in $users) {
    $results += Test-Login -Username $user.Username -Password $user.Password
}

# Test invalid credentials
Write-Host "`n--- Testing Invalid Credentials ---" -ForegroundColor Magenta
$results += !(Test-Login -Username "admin" -Password "wrongpassword")
$results += !(Test-Login -Username "invaliduser" -Password "password123")

# Summary
Write-Host "`n============================================================" -ForegroundColor Cyan
$passCount = ($results | Where-Object { $_ -eq $true }).Count
$totalCount = $results.Count
$failCount = $totalCount - $passCount

Write-Host "Total: $totalCount | Passed: $passCount | Failed: $failCount" -ForegroundColor White

if ($failCount -eq 0) {
    Write-Host "✓ All authentication tests passed!" -ForegroundColor Green
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
}

Write-Host ""
