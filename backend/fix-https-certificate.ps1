<#
.SYNOPSIS
    Fix HTTPS development certificate installation issue
.DESCRIPTION
    This script helps resolve "Access denied to store" error when Visual Studio
    tries to install the development certificate for HTTPS.

    IMPORTANT: Run this script as Administrator!
#>

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host ""
    Write-Host "To run as Administrator:" -ForegroundColor Yellow
    Write-Host "  1. Right-click on PowerShell" -ForegroundColor Yellow
    Write-Host "  2. Select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host "  3. Navigate to: $PSScriptRoot" -ForegroundColor Yellow
    Write-Host "  4. Run: .\fix-https-certificate.ps1" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Host "================================" -ForegroundColor Cyan
Write-Host "HTTPS Certificate Fix Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean existing certificates
Write-Host "[1/3] Cleaning existing development certificates..." -ForegroundColor Yellow
try {
    dotnet dev-certs https --clean
    Write-Host "✓ Certificates cleaned successfully" -ForegroundColor Green
} catch {
    Write-Host "⚠ Warning: Could not clean certificates: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""

# Step 2: Create new certificate
Write-Host "[2/3] Creating new development certificate..." -ForegroundColor Yellow
try {
    dotnet dev-certs https --trust
    Write-Host "✓ Certificate created and trusted successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Error creating certificate: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "If you still get 'Access Denied' errors, try:" -ForegroundColor Yellow
    Write-Host "  - Close all Visual Studio instances" -ForegroundColor Yellow
    Write-Host "  - Restart your computer" -ForegroundColor Yellow
    Write-Host "  - Run this script again" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Step 3: Verify certificate
Write-Host "[3/3] Verifying certificate installation..." -ForegroundColor Yellow
try {
    $result = dotnet dev-certs https --check --trust
    Write-Host "✓ Certificate verification complete" -ForegroundColor Green
} catch {
    Write-Host "⚠ Warning: Could not verify certificate: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================" -ForegroundColor Green
Write-Host "Certificate installation complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Restart Visual Studio" -ForegroundColor White
Write-Host "  2. Launch your application with the 'https' profile" -ForegroundColor White
Write-Host "  3. Test with: .\src\PocSSE.Backend.WebApi\test-api.ps1" -ForegroundColor White
Write-Host ""
