#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Chess of Cards Local Test Server for WebSocket testing.

.DESCRIPTION
    This script starts the local test server which allows you to test
    WebSocket connections, game actions, and Lambda handlers locally
    without deploying to AWS.

.PARAMETER Port
    The port to run the server on. Default is 5000.

.PARAMETER Rebuild
    If specified, rebuilds the solution before starting the server.

.PARAMETER Watch
    If specified, runs in watch mode with auto-reload on file changes.

.EXAMPLE
    .\scripts\run-local-server.ps1
    Starts the server on port 5000

.EXAMPLE
    .\scripts\run-local-server.ps1 -Port 8080
    Starts the server on port 8080

.EXAMPLE
    .\scripts\run-local-server.ps1 -Rebuild
    Rebuilds the solution and starts the server

.EXAMPLE
    .\scripts\run-local-server.ps1 -Watch
    Starts the server in watch mode (auto-reload on changes)
#>

param(
    [int]$Port = 5000,
    [switch]$Rebuild,
    [switch]$Watch
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Color output functions
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }
function Write-Error { Write-Host $args -ForegroundColor Red }

# Print banner
Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  Chess of Cards - Local Test Server   " -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
Write-Host ""

# Determine project root (script is in scripts/ folder)
$ProjectRoot = Split-Path $PSScriptRoot -Parent

# Check if we're in the correct directory
if (-not (Test-Path "$ProjectRoot/ChessOfCards.sln")) {
    Write-Error "Error: ChessOfCards.sln not found at $ProjectRoot"
    Write-Error "This script should be run from the project root or scripts folder."
    exit 1
}

# Check AWS credentials
Write-Info "Checking AWS credentials..."
try {
    $null = aws sts get-caller-identity 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "WARNING: AWS credentials may not be configured properly"
        Write-Warning "Run 'aws configure' to set up your credentials"
        Write-Host ""
    } else {
        Write-Success "AWS credentials OK"
    }
} catch {
    Write-Warning "WARNING: AWS CLI not found or credentials not configured"
    Write-Host ""
}

# Rebuild if requested
if ($Rebuild) {
    Write-Info "Building solution..."
    try {
        Push-Location $ProjectRoot
        dotnet build ChessOfCards.sln
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed with exit code $LASTEXITCODE"
            exit $LASTEXITCODE
        }
        Write-Success "Build completed successfully!"
        Pop-Location
    }
    catch {
        Write-Error "Build failed: $_"
        exit 1
    }
    Write-Host ""
}

# Set environment variables for local testing
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:IS_LOCAL_TESTING = "true"
$env:ASPNETCORE_URLS = "http://localhost:$Port"

Write-Info "Configuration:"
Write-Host "  Server URL:      http://localhost:$Port" -ForegroundColor White
Write-Host "  WebSocket:       ws://localhost:$Port/ws" -ForegroundColor White
Write-Host "  Environment:     Development" -ForegroundColor White
Write-Host "  Watch Mode:      $Watch" -ForegroundColor White
Write-Host ""

if ($Watch) {
    Write-Warning "Running in WATCH mode - server will auto-reload on file changes"
} else {
    Write-Info "Tip: Use -Watch flag for auto-reload on file changes"
}

Write-Host ""
Write-Warning "Press Ctrl+C to stop the server"
Write-Host ""

# Start the server
try {
    Push-Location "$ProjectRoot/tools/ChessOfCards.LocalTestServer"

    if ($Watch) {
        dotnet watch run --no-hot-reload
    } else {
        if ($Rebuild) {
            dotnet run --no-build
        } else {
            dotnet run
        }
    }
}
catch {
    Write-Error "Server failed to start: $_"
    exit 1
}
finally {
    Pop-Location
}
