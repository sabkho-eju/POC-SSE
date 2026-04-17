<#
.SYNOPSIS
    Test Server-Sent Events (SSE) endpoint with JWT authentication
.DESCRIPTION
    Connects to the SSE stream endpoint, authenticates with JWT, and listens for real-time events.
#>

param(
    [string]$BaseUrl = "http://localhost:5236",
    [string]$Username = "admin",
    [string]$Password = "password123",
    [string]$ClientId = "test-client-$(Get-Random -Minimum 1000 -Maximum 9999)"
)

$ErrorActionPreference = "Stop"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  SSE Stream Test" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor White
Write-Host "Username: $Username" -ForegroundColor White
Write-Host "Client ID: $ClientId" -ForegroundColor White
Write-Host ""

# STEP 1: Login
Write-Host "[1/3] Authenticating..." -ForegroundColor Yellow

try {
    $loginResponse = Invoke-RestMethod `
        -Uri "$BaseUrl/api/authentication/login" `
        -Method POST `
        -Body (@{ username = $Username; password = $Password } | ConvertTo-Json) `
        -ContentType "application/json"
    
    if (-not $loginResponse.success) {
        Write-Host "✗ Authentication failed: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
    
    $token = $loginResponse.token
    Write-Host "✓ Authenticated as $Username" -ForegroundColor Green
}
catch {
    Write-Host "✗ Authentication error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# STEP 2: Connect to SSE
Write-Host "`n[2/3] Connecting to SSE stream..." -ForegroundColor Yellow

$sseUrl = "$BaseUrl/api/serviceeventnotification/ssestream?clientId=$ClientId"
Write-Host "URL: $sseUrl" -ForegroundColor Gray
Write-Host ""

try {
    $uri = [Uri]$sseUrl
    $request = [System.Net.HttpWebRequest]::Create($uri)
    $request.Method = "GET"
    $request.Headers.Add("Authorization", "Bearer $token")
    $request.Accept = "text/event-stream"
    $request.AllowReadStreamBuffering = $false
    $request.KeepAlive = $true
    
    $response = $request.GetResponse()
    Write-Host "✓ Connected (HTTP $([int]$response.StatusCode))" -ForegroundColor Green
    
    $stream = $response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    
    Write-Host "`n[3/3] Listening for events (Press Ctrl+C to stop)..." -ForegroundColor Yellow
    Write-Host "============================================================" -ForegroundColor Gray
    
    $eventCount = 0
    $startTime = Get-Date
    $currentEvent = @{ Type = $null; Data = $null; Id = $null }

    while ($true) {
        $line = $reader.ReadLine()
        
        if ($null -eq $line) {
            Write-Host "`n✗ Connection closed by server" -ForegroundColor Red
            break
        }
        
        if ($line -eq "") {
            if ($currentEvent.Type -or $currentEvent.Data) {
                $eventCount++
                $timestamp = Get-Date -Format "HH:mm:ss.fff"
                
                Write-Host "`n[$timestamp] EVENT: " -NoNewline -ForegroundColor DarkGray
                Write-Host $currentEvent.Type -ForegroundColor Yellow
                
                if ($currentEvent.Data) {
                    try {
                        $json = $currentEvent.Data | ConvertFrom-Json
                        $json | ConvertTo-Json -Depth 5 | Write-Host -ForegroundColor White
                    } catch {
                        Write-Host "  $($currentEvent.Data)" -ForegroundColor White
                    }
                }
                
                $currentEvent = @{ Type = $null; Data = $null; Id = $null }
            }
            continue
        }
        
        if ($line.StartsWith("event:")) {
            $currentEvent.Type = $line.Substring(6).Trim()
        }
        elseif ($line.StartsWith("data:")) {
            $currentEvent.Data = $line.Substring(5).Trim()
        }
        elseif ($line.StartsWith("id:")) {
            $currentEvent.Id = $line.Substring(3).Trim()
        }
        elseif ($line.StartsWith(":")) {
            $timestamp = Get-Date -Format "HH:mm:ss.fff"
            Write-Host "[$timestamp] PING (keep-alive)" -ForegroundColor DarkCyan
        }
    }
}
catch {
    Write-Host "`n✗ Error: $($_.Exception.Message)" -ForegroundColor Red
}
finally {
    if ($reader) { $reader.Close() }
    if ($stream) { $stream.Close() }
    if ($response) { $response.Close() }
    
    Write-Host "`n============================================================" -ForegroundColor Gray
    Write-Host "Events received: $eventCount" -ForegroundColor White
    if ($startTime) {
        $duration = (Get-Date) - $startTime
        Write-Host "Duration: $($duration.TotalSeconds.ToString('F2'))s" -ForegroundColor White
    }
    Write-Host ""
}