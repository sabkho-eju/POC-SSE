<#
.SYNOPSIS
    Send test events to SSE clients
.DESCRIPTION
    Sends notifications to connected SSE clients for testing
#>

param(
    [string]$BaseUrl = "http://localhost:5236",
    [string]$Username = "admin",
    [string]$Password = "password123",
    [Parameter(Mandatory=$false)]
    [string]$ClientId,
    [string]$EventName = "test-event",
    [string]$JobId = "test-job-$(Get-Random)",
    [string]$Message = "Test message",
    [string]$Status = "Running",
    [switch]$Broadcast
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Send SSE Event" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

# Login
Write-Host "Authenticating..." -ForegroundColor Yellow
$loginResponse = Invoke-RestMethod `
    -Uri "$BaseUrl/api/authentication/login" `
    -Method POST `
    -Body (@{ username = $Username; password = $Password } | ConvertTo-Json) `
    -ContentType "application/json"

if (-not $loginResponse.success) {
    Write-Host "✗ Authentication failed" -ForegroundColor Red
    exit 1
}

$token = $loginResponse.token
Write-Host "✓ Authenticated" -ForegroundColor Green

# Prepare notification
$notification = @{
    clientId = $ClientId
    eventName = $EventName
    jobId = $JobId
    message = $Message
    status = $Status
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Send event
Write-Host "`nSending event..." -ForegroundColor Yellow

try {
    if ($Broadcast) {
        Write-Host "Mode: BROADCAST to all clients" -ForegroundColor Cyan
        $url = "$BaseUrl/api/serviceeventnotification/broadcast"
    }
    else {
        if (-not $ClientId) {
            Write-Host "✗ ClientId required for targeted send" -ForegroundColor Red
            Write-Host "Use -Broadcast for broadcasting to all clients" -ForegroundColor Yellow
            exit 1
        }
        Write-Host "Mode: Send to client '$ClientId'" -ForegroundColor Cyan
        $url = "$BaseUrl/api/serviceeventnotification/send"
    }

    $response = Invoke-RestMethod `
        -Uri $url `
        -Method POST `
        -Headers $headers `
        -Body ($notification | ConvertTo-Json)

    Write-Host "✓ Event sent successfully" -ForegroundColor Green
    Write-Host ""
    $response | ConvertTo-Json | Write-Host -ForegroundColor White
}
catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host $_.ErrorDetails.Message -ForegroundColor Red
    }
}

Write-Host ""