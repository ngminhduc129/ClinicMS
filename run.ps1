$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$env:DOTNET_CLI_HOME = Join-Path $root ".dotnet_home"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:NUGET_PACKAGES = Join-Path $root ".nuget\\packages"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

if (-not (Test-Path $env:DOTNET_CLI_HOME)) {
    New-Item -ItemType Directory -Path $env:DOTNET_CLI_HOME | Out-Null
}

if (-not (Test-Path $env:NUGET_PACKAGES)) {
    New-Item -ItemType Directory -Path $env:NUGET_PACKAGES -Force | Out-Null
}

Write-Host "Starting ClinicMS..."
dotnet run --project ClinicMS.csproj
