<#
.SYNOPSIS
    Starts each HTTP service in Development mode and writes its OpenAPI document.

.DESCRIPTION
    The services only expose Swagger in Development, so this script starts the built
    assemblies on loopback ports, downloads /swagger/v1/swagger.json and stops them.
    The databases are temporary SQLite files under %TEMP%\marketlane-openapi.

.PARAMETER OutputDirectory
    Directory for the generated JSON files. Defaults to openapi/ at the repository root.

.PARAMETER SkipBuild
    Reuse the existing Debug build instead of running dotnet build first.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts/export-openapi.ps1
#>
[CmdletBinding()]
param(
    [string]$OutputDirectory = '',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'openapi'
}
$configuration = 'Debug'
$tempData = Join-Path $env:TEMP 'marketlane-openapi'
$logDirectory = Join-Path $env:TEMP 'marketlane-openapi-logs'

if (-not $SkipBuild) {
    Write-Host 'Building the solution...'
    & dotnet build (Join-Path $repoRoot 'ECommerce.sln') -c $configuration -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet build failed.'
    }
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $tempData | Out-Null
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null

Get-ChildItem -Path $tempData -Filter '*.db*' -ErrorAction SilentlyContinue | Remove-Item -Force

$developmentSecret = 'openapi-export-development-secret-that-is-long-enough-1234567890'
$developmentInternalKey = 'openapi-export-development-internal-key-that-is-long-enough'
$databaseProvider = 'Sqlite'

$services = @(
    @{
        Name = 'product-service'
        Project = 'src/ECommerce.ProductService'
        Assembly = 'ECommerce.ProductService.dll'
        Port = 5101
        Settings = @{
            'ConnectionStrings__ProductDb' = "Data Source=$(Join-Path $tempData 'product.db')"
            'Database__Provider' = $databaseProvider
            'Database__ApplyMigrations' = 'true'
        }
    },
    @{
        Name = 'user-service'
        Project = 'src/ECommerce.UserService'
        Assembly = 'ECommerce.UserService.dll'
        Port = 5102
        Settings = @{
            'ConnectionStrings__UserDb' = "Data Source=$(Join-Path $tempData 'user.db')"
            'Database__Provider' = $databaseProvider
            'Database__ApplyMigrations' = 'true'
        }
    },
    @{
        Name = 'order-service'
        Project = 'src/ECommerce.OrderService'
        Assembly = 'ECommerce.OrderService.dll'
        Port = 5103
        Settings = @{
            'ConnectionStrings__OrderDb' = "Data Source=$(Join-Path $tempData 'order.db')"
            'Database__Provider' = $databaseProvider
            'Database__ApplyMigrations' = 'true'
            'Services__ProductServiceUrl' = 'http://127.0.0.1:5101/'
        }
    },
    @{
        Name = 'gateway'
        Project = 'src/ECommerce.Gateway'
        Assembly = 'ECommerce.Gateway.dll'
        Port = 5080
        Settings = @{
            'Services__ProductServiceUrl' = 'http://127.0.0.1:5101'
            'Services__UserServiceUrl' = 'http://127.0.0.1:5102'
            'Services__OrderServiceUrl' = 'http://127.0.0.1:5103'
        }
    }
)

$sharedSettings = @{
    'ASPNETCORE_ENVIRONMENT' = 'Development'
    'Jwt__Secret' = $developmentSecret
    'Jwt__Issuer' = 'ECommerce'
    'Jwt__Audience' = 'ECommerce.Client'
    'Internal__ApiKey' = $developmentInternalKey
    'Foundry__Endpoint' = ''
    'Foundry__ApiKey' = ''
    'Foundry__ChatDeployment' = ''
    'Foundry__Mode' = 'auto'
    'Logging__LogLevel__Default' = 'Warning'
    'Logging__LogLevel__Microsoft.AspNetCore' = 'Warning'
}

$originalEnvironment = @{}
foreach ($name in ($sharedSettings.Keys + ($services | ForEach-Object { $_.Settings.Keys }) | Select-Object -Unique)) {
    $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

try {
    foreach ($service in $services) {
        foreach ($pair in $sharedSettings.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($pair.Key, $pair.Value, 'Process')
        }
        foreach ($pair in $service.Settings.GetEnumerator()) {
            [Environment]::SetEnvironmentVariable($pair.Key, $pair.Value, 'Process')
        }

        [Environment]::SetEnvironmentVariable(
            'ASPNETCORE_URLS',
            "http://127.0.0.1:$($service.Port)",
            'Process')

        $projectDirectory = Join-Path $repoRoot $service.Project
        $assemblyPath = Join-Path $projectDirectory "bin/$configuration/net10.0/$($service.Assembly)"
        if (-not (Test-Path $assemblyPath)) {
            throw "Build output not found: $assemblyPath"
        }

        $stdout = Join-Path $logDirectory "$($service.Name).out.log"
        $stderr = Join-Path $logDirectory "$($service.Name).err.log"
        Write-Host "Starting $($service.Name) on port $($service.Port)..."

        $process = Start-Process `
            -FilePath 'dotnet' `
            -ArgumentList @("`"$assemblyPath`"") `
            -WorkingDirectory $projectDirectory `
            -RedirectStandardOutput $stdout `
            -RedirectStandardError $stderr `
            -PassThru

        try {
            $swaggerUrl = "http://127.0.0.1:$($service.Port)/swagger/v1/swagger.json"
            $document = $null
            $deadline = (Get-Date).AddSeconds(90)

            while ((Get-Date) -lt $deadline) {
                if ($process.HasExited) {
                    $tail = Get-Content $stderr -Tail 20 -ErrorAction SilentlyContinue
                    throw "$($service.Name) exited before Swagger was available. $tail"
                }

                try {
                    $document = Invoke-RestMethod -Uri $swaggerUrl -TimeoutSec 2
                    break
                }
                catch {
                    Start-Sleep -Milliseconds 500
                }
            }

            if ($null -eq $document) {
                throw "Timed out waiting for $swaggerUrl."
            }

            $target = Join-Path $OutputDirectory "$($service.Name).json"
            $document | ConvertTo-Json -Depth 100 | Set-Content -Path $target -Encoding UTF8
            Write-Host "Wrote $target"
        }
        finally {
            if (-not $process.HasExited) {
                & taskkill /PID $process.Id /T /F 2>$null | Out-Null
            }
        }
    }
}
finally {
    foreach ($pair in $originalEnvironment.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($pair.Key, $pair.Value, 'Process')
    }
}
