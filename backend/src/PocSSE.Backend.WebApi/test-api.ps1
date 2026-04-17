<#
.SYNOPSIS
    Test script for PocSSE API endpoints with JWT Authentication
.DESCRIPTION
    Provides detailed feedback for each API call. Authenticates first to obtain JWT token.
#>

param(
    [string]$BaseUrl = "http://localhost:5236",  # Using HTTP by default to avoid certificate issues. Use -BaseUrl "https://localhost:7084" for HTTPS
    [string]$Username = "testuser",                 # Available users: admin, user1, testuser, demo
    [string]$Password = "testpassword"            # Passwords: password123, pass123, testpassword, demo
)

$ErrorActionPreference = "Stop"
$Script:AuthToken = $null

function Write-TestHeader {
    param([string]$Text)
    Write-Host "`n$('=' * 60)" -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host "$('=' * 60)" -ForegroundColor Cyan
}

function Invoke-ApiTest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [hashtable]$Body = $null,
        [string]$Description,
        [switch]$UseAuth = $true,
        [switch]$ReturnResponse = $false
    )

    Write-Host "`n[$Method] $Endpoint" -ForegroundColor Yellow
    if ($Description) {
        Write-Host "Description: $Description" -ForegroundColor Gray
    }

    $url = "$BaseUrl$Endpoint"
    Write-Host "URL: $url" -ForegroundColor DarkGray

    try {
        $params = @{
            Uri = $url
            Method = $Method
            TimeoutSec = 30
        }

        # Add Authorization header if token is available and UseAuth is true
        if ($UseAuth -and $Script:AuthToken) {
            $params.Headers = @{
                "Authorization" = "Bearer $Script:AuthToken"
            }
            Write-Host "Using JWT Authentication" -ForegroundColor DarkCyan
        }

        if ($Body) {
            $jsonBody = $Body | ConvertTo-Json -Depth 10
            $params.Body = $jsonBody
            $params.ContentType = "application/json"
            Write-Host "`nRequest Body:" -ForegroundColor Magenta
            Write-Host $jsonBody -ForegroundColor DarkGray
        }

        Write-Host "`nSending request..." -ForegroundColor Gray
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod @params
        $stopwatch.Stop()

        Write-Host "✓ SUCCESS (${stopwatch.ElapsedMilliseconds}ms)" -ForegroundColor Green
        Write-Host "`nResponse:" -ForegroundColor White
        $response | ConvertTo-Json -Depth 10 | Write-Host -ForegroundColor White

        if ($ReturnResponse) {
            return $response
        }

        return $true
    }
    catch {
        Write-Host "✗ FAILED" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red

        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            Write-Host "Status Code: $statusCode" -ForegroundColor Red
        }

        if ($_.ErrorDetails) {
            Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
        }

        if ($ReturnResponse) {
            return $null
        }

        return $false
    }
}

# Main execution
Write-TestHeader "PocSSE API Test Suite with JWT Authentication"
Write-Host "Base URL: $BaseUrl" -ForegroundColor White
Write-Host "Username: $Username" -ForegroundColor White
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White

$results = @()

# Test 0: Authentication - Login
Write-TestHeader "Authentication Tests"

$loginResponse = Invoke-ApiTest `
    -Method POST `
    -Endpoint "/api/authentication/login" `
    -Body @{
        username = $Username
        password = $Password
    } `
    -Description "Login to obtain JWT token" `
    -UseAuth:$false `
    -ReturnResponse

if ($loginResponse -and $loginResponse.success) {
    $Script:AuthToken = $loginResponse.token
    Write-Host "`n✓ Authentication successful! Token acquired." -ForegroundColor Green
    Write-Host "Token (first 20 chars): $($Script:AuthToken.Substring(0, [Math]::Min(20, $Script:AuthToken.Length)))..." -ForegroundColor DarkGray
    Write-Host "Logged in as: $($loginResponse.username)" -ForegroundColor Cyan
    $results += $true
} else {
    Write-Host "`n✗ Authentication failed! Cannot proceed with authenticated requests." -ForegroundColor Red
    $results += $false
}

# Test 1: GET Test endpoint
Write-TestHeader "API Endpoint Tests"

$results += Invoke-ApiTest `
    -Method GET `
    -Endpoint "/api/jobprocessing/test" `
    -Description "Test endpoint to verify controller is working"

# Test 2: POST Process Job
$results += Invoke-ApiTest `
    -Method POST `
    -Endpoint "/api/jobprocessing/process" `
    -Body @{
        jobId = "job-$(Get-Date -Format 'yyyyMMddHHmmss')"
        jobData = "Sample job data from PowerShell"
    } `
    -Description "Process a job with sample data"

# Test 3: Logout
Write-TestHeader "Logout Test"

$results += Invoke-ApiTest `
    -Method POST `
    -Endpoint "/api/authentication/logout" `
    -Description "Logout (JWT token will be discarded client-side)"

if ($results[-1]) {
    Write-Host "`n✓ Logged out successfully. Token discarded." -ForegroundColor Green
    $Script:AuthToken = $null
}

# Summary
Write-TestHeader "Test Summary"
$successCount = ($results | Where-Object { $_ -eq $true }).Count
$totalCount = $results.Count
$failCount = $totalCount - $successCount

Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "Passed: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor $(if ($failCount -gt 0) { "Red" } else { "Green" })

if ($failCount -eq 0) {
    Write-Host "`n✓ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "`n✗ Some tests failed. Check the output above." -ForegroundColor Red
}

Write-Host ""