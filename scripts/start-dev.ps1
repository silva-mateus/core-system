<#
.SYNOPSIS
    Generic dev environment starter for core-system projects.

.DESCRIPTION
    Launches Docker containers (if configured), the .NET backend API, and the
    frontend dev server (Next.js or Vite). All output is streamed into a single
    terminal with colored prefixes [API] and [WEB].

    Press Ctrl+C to stop all services.

    This script is meant to be called from a thin wrapper in each consuming
    project -- do not run it directly.

.NOTES
    Location : core-system/scripts/start-dev.ps1
    Called by: <project>/start-dev.ps1  (thin wrapper, ~20 lines)
#>

param(
    # ── Project identification ───────────────────────────────────────────
    [Parameter(Mandatory)]
    [string]$ProjectName,

    [Parameter(Mandatory)]
    [string]$ProjectRoot,

    # ── Backend ──────────────────────────────────────────────────────────
    [string]$BackendPath = "backend",

    [Parameter(Mandatory)]
    [string]$BackendCsproj,

    [string]$ApiPortDefault = "5000",

    [string]$SwaggerPath = "/swagger",

    [string]$HealthPath,

    [string]$LogDir,

    # ── Frontend ─────────────────────────────────────────────────────────
    [string]$FrontendPath = "frontend",

    [ValidateSet("next", "vite")]
    [string]$FrontendRunner = "next",

    [string]$FePortDefault = "3000",

    # ── Docker ───────────────────────────────────────────────────────────
    [ValidateSet("external", "compose", "none")]
    [string]$DockerMode = "none",

    [string]$DockerContainerName,

    [string]$DockerComposeFile,

    [string[]]$DockerComposeServices,

    # ── Extra environment variables passed to backend / frontend jobs ────
    [hashtable]$BackendEnv = @{},

    [hashtable]$FrontendEnv = @{},

    # ── Runtime switches (forwarded from the project wrapper) ────────────
    [switch]$SkipDocker,
    [switch]$BackendOnly,
    [switch]$FrontendOnly
)

$ErrorActionPreference = "Stop"

# ═══════════════════════════════════════════════════════════════════════════
#  Helpers
# ═══════════════════════════════════════════════════════════════════════════

function Write-Header($msg) { Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Write-Ok($msg)     { Write-Host "  [OK] $msg"    -ForegroundColor Green }
function Write-Info($msg)   { Write-Host "  [..] $msg"    -ForegroundColor Yellow }
function Write-Err($msg)    { Write-Host "  [!!] $msg"    -ForegroundColor Red }

# ═══════════════════════════════════════════════════════════════════════════
#  Load .env (if present in the project root)
# ═══════════════════════════════════════════════════════════════════════════

$envFile = Join-Path $ProjectRoot ".env"
if (Test-Path $envFile) {
    Write-Info "Loading .env from project root"
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line -split "=", 2
            if ($parts.Length -eq 2) {
                $key = $parts[0].Trim()
                $val = $parts[1].Trim()
                if (-not [Environment]::GetEnvironmentVariable($key, "Process")) {
                    [Environment]::SetEnvironmentVariable($key, $val, "Process")
                }
            }
        }
    }
    Write-Ok ".env loaded."
}

# ═══════════════════════════════════════════════════════════════════════════
#  Resolve ports  (env vars override defaults)
# ═══════════════════════════════════════════════════════════════════════════

$apiPort      = if ($env:API_PORT)      { $env:API_PORT }      else { $ApiPortDefault }
$frontendPort = if ($env:FRONTEND_PORT) { $env:FRONTEND_PORT } else { $FePortDefault }

if ($env:SKIP_DOCKER -eq "true" -and -not $SkipDocker) { $SkipDocker = $true }

# ═══════════════════════════════════════════════════════════════════════════
#  Kill stale processes from previous runs
# ═══════════════════════════════════════════════════════════════════════════

function Stop-StaleProcesses {
    Write-Header "Checking for stale processes"

    $staleApiPids = netstat -ano 2>$null |
        Select-String ":$apiPort\s" |
        ForEach-Object { ($_ -split '\s+')[-1] } |
        Where-Object { $_ -match '^\d+$' } |
        Sort-Object -Unique

    foreach ($stalePid in $staleApiPids) {
        try {
            $proc = Get-Process -Id ([int]$stalePid) -ErrorAction SilentlyContinue
            if ($proc -and $proc.ProcessName -eq "dotnet") {
                Write-Info "Killing stale dotnet on port $apiPort (PID: $stalePid)"
                Stop-Process -Id ([int]$stalePid) -Force -ErrorAction SilentlyContinue
            }
        } catch { }
    }

    $staleFePids = netstat -ano 2>$null |
        Select-String ":$frontendPort\s" |
        ForEach-Object { ($_ -split '\s+')[-1] } |
        Where-Object { $_ -match '^\d+$' } |
        Sort-Object -Unique

    foreach ($stalePid in $staleFePids) {
        try {
            $proc = Get-Process -Id ([int]$stalePid) -ErrorAction SilentlyContinue
            if ($proc -and $proc.ProcessName -eq "node") {
                Write-Info "Killing stale node on port $frontendPort (PID: $stalePid)"
                Stop-Process -Id ([int]$stalePid) -Force -ErrorAction SilentlyContinue
            }
        } catch { }
    }

    $orphanedDotnet = Get-Process -Name dotnet -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue).CommandLine
                $cmdLine -and $cmdLine -like "*$ProjectName*"
            } catch { $false }
        }

    foreach ($proc in $orphanedDotnet) {
        Write-Info "Killing orphaned dotnet (PID: $($proc.Id))"
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    Get-Job -Name "Backend-API"  -ErrorAction SilentlyContinue | Stop-Job -PassThru -ErrorAction SilentlyContinue | Remove-Job -Force -ErrorAction SilentlyContinue
    Get-Job -Name "Frontend-Dev" -ErrorAction SilentlyContinue | Stop-Job -PassThru -ErrorAction SilentlyContinue | Remove-Job -Force -ErrorAction SilentlyContinue

    Write-Ok "Stale process check complete."
}

Stop-StaleProcesses

# ═══════════════════════════════════════════════════════════════════════════
#  Job tracking & cleanup
# ═══════════════════════════════════════════════════════════════════════════

$jobs = @()

function Cleanup {
    Write-Header "Stopping services"

    foreach ($j in $script:jobs) {
        if ($j -and (Get-Job -Id $j.Id -ErrorAction SilentlyContinue)) {
            Write-Info "Stopping $($j.Name)..."
            Stop-Job  -Id $j.Id -ErrorAction SilentlyContinue
            Remove-Job -Id $j.Id -Force -ErrorAction SilentlyContinue
        }
    }

    $childDotnet = Get-Process -Name dotnet -ErrorAction SilentlyContinue |
        Where-Object {
            try {
                $cmdLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)" -ErrorAction SilentlyContinue).CommandLine
                $cmdLine -and $cmdLine -like "*$script:ProjectName*"
            } catch { $false }
        }
    foreach ($proc in $childDotnet) {
        Write-Info "Stopping child dotnet (PID: $($proc.Id))"
        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    }

    Write-Ok "All services stopped."
}

Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Cleanup } -ErrorAction SilentlyContinue | Out-Null

# ═══════════════════════════════════════════════════════════════════════════
#  1. Docker
# ═══════════════════════════════════════════════════════════════════════════

if (-not $SkipDocker -and -not $FrontendOnly -and $DockerMode -ne "none") {
    Write-Header "Docker ($DockerMode)"

    $dockerAvailable = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerAvailable) {
        Write-Err "Docker not found in PATH. Install Docker Desktop or use -SkipDocker."
        exit 1
    }

    function Wait-ContainerHealthy([string]$containerName, [int]$timeoutSeconds = 60) {
        $waited = 0
        while ($waited -lt $timeoutSeconds) {
            $health = docker inspect --format="{{.State.Health.Status}}" $containerName 2>$null
            if ($health -eq "healthy") { return $true }
            if (-not $health) { return $true }
            Start-Sleep -Seconds 2
            $waited += 2
            Write-Host "." -NoNewline -ForegroundColor DarkGray
        }
        Write-Host ""
        return $false
    }

    switch ($DockerMode) {
        "external" {
            if (-not $DockerContainerName) {
                Write-Err "DockerMode 'external' requires -DockerContainerName."
                exit 1
            }

            $running = docker ps --filter "name=^${DockerContainerName}$" --format "{{.Names}}" 2>$null
            if ($running -eq $DockerContainerName) {
                $health = docker inspect --format="{{.State.Health.Status}}" $DockerContainerName 2>$null
                if ($health -eq "healthy") {
                    Write-Ok "$DockerContainerName is running and healthy."
                }
                elseif ($health) {
                    Write-Info "$DockerContainerName running (status: $health). Waiting..."
                    if (-not (Wait-ContainerHealthy $DockerContainerName)) {
                        Write-Err "$DockerContainerName did not become healthy within 60s."
                        exit 1
                    }
                    Write-Ok "$DockerContainerName is ready."
                }
                else {
                    Write-Ok "$DockerContainerName is running (no health check configured)."
                }
            }
            else {
                Write-Err "Container '$DockerContainerName' is not running."
                Write-Info "Start it manually before running this script."
                exit 1
            }
        }

        "compose" {
            $composeArgs = @("compose")

            if ($DockerComposeFile) {
                $composePath = if ([System.IO.Path]::IsPathRooted($DockerComposeFile)) {
                    $DockerComposeFile
                } else {
                    Join-Path $ProjectRoot $DockerComposeFile
                }
                $composeArgs += @("-f", $composePath)
            }

            $composeArgs += @("up", "-d")

            if ($DockerComposeServices) {
                $composeArgs += $DockerComposeServices
            }

            Write-Info "Running: docker $($composeArgs -join ' ')"
            Push-Location $ProjectRoot
            & docker @composeArgs
            Pop-Location

            if ($DockerContainerName) {
                Write-Info "Waiting for $DockerContainerName to be healthy..."
                if (-not (Wait-ContainerHealthy $DockerContainerName)) {
                    Write-Err "$DockerContainerName did not become healthy within 60s."
                    exit 1
                }
                Write-Ok "$DockerContainerName is ready."
            }
            else {
                Write-Ok "Docker Compose services started."
            }
        }
    }
}

# ═══════════════════════════════════════════════════════════════════════════
#  2. Backend (.NET API)
# ═══════════════════════════════════════════════════════════════════════════

if (-not $FrontendOnly) {
    Write-Header "Backend API (.NET)"

    $backendFullPath = if ([System.IO.Path]::IsPathRooted($BackendPath)) {
        $BackendPath
    } else {
        Join-Path $ProjectRoot $BackendPath
    }

    $csprojPath = Join-Path $backendFullPath $BackendCsproj
    if (-not (Test-Path $csprojPath)) {
        Write-Err "Backend project not found: $csprojPath"
        exit 1
    }

    if ($LogDir) {
        $logFullPath = Join-Path $backendFullPath $LogDir
        if (-not (Test-Path $logFullPath)) {
            New-Item -ItemType Directory -Path $logFullPath -Force | Out-Null
        }
    }

    Write-Info "Starting backend on http://localhost:$apiPort ..."

    $beEnv = @{}
    $beEnv["Cors__AllowedOrigins__0"] = "http://localhost:$frontendPort"
    foreach ($key in $BackendEnv.Keys) { $beEnv[$key] = $BackendEnv[$key] }

    $backendJob = Start-Job -Name "Backend-API" -ScriptBlock {
        param($path, $port, $envVars)
        Set-Location $path
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $env:ASPNETCORE_URLS = "http://localhost:$port"
        foreach ($key in $envVars.Keys) {
            [Environment]::SetEnvironmentVariable($key, $envVars[$key], "Process")
        }
        dotnet run --no-launch-profile 2>&1
    } -ArgumentList $backendFullPath, $apiPort, $beEnv

    $jobs += $backendJob

    Start-Sleep -Seconds 5

    if ($backendJob.State -eq "Failed") {
        Write-Err "Backend failed to start:"
        Receive-Job -Id $backendJob.Id
        Cleanup
        exit 1
    }
    Write-Ok "Backend job started."
}

# ═══════════════════════════════════════════════════════════════════════════
#  3. Frontend (Next.js / Vite)
# ═══════════════════════════════════════════════════════════════════════════

if (-not $BackendOnly) {
    $runnerLabel = if ($FrontendRunner -eq "next") { "Next.js" } else { "Vite" }
    Write-Header "Frontend ($runnerLabel)"

    $frontendFullPath = if ([System.IO.Path]::IsPathRooted($FrontendPath)) {
        $FrontendPath
    } else {
        Join-Path $ProjectRoot $FrontendPath
    }

    if (-not (Test-Path (Join-Path $frontendFullPath "package.json"))) {
        Write-Err "Frontend not found: $frontendFullPath"
        exit 1
    }

    if (-not (Test-Path (Join-Path $frontendFullPath "node_modules"))) {
        Write-Info "Installing npm dependencies..."
        Push-Location $frontendFullPath
        npm install
        Pop-Location
    }

    Write-Info "Starting frontend on http://localhost:$frontendPort ..."

    $feEnv = @{}
    $feEnv["API_PROXY_TARGET"] = "http://localhost:$apiPort"

    switch ($FrontendRunner) {
        "next" { $feEnv["PORT"] = $frontendPort }
        "vite" { $feEnv["VITE_DEV_PORT"] = $frontendPort }
    }

    foreach ($key in $FrontendEnv.Keys) { $feEnv[$key] = $FrontendEnv[$key] }

    $frontendJob = Start-Job -Name "Frontend-Dev" -ScriptBlock {
        param($path, $envVars)
        Set-Location $path
        foreach ($key in $envVars.Keys) {
            [Environment]::SetEnvironmentVariable($key, $envVars[$key], "Process")
        }
        npm run dev 2>&1
    } -ArgumentList $frontendFullPath, $feEnv

    $jobs += $frontendJob

    Start-Sleep -Seconds 3

    if ($frontendJob.State -eq "Failed") {
        Write-Err "Frontend failed to start:"
        Receive-Job -Id $frontendJob.Id
        Cleanup
        exit 1
    }
    Write-Ok "Frontend job started."
}

# ═══════════════════════════════════════════════════════════════════════════
#  Summary
# ═══════════════════════════════════════════════════════════════════════════

Write-Header "$ProjectName - Development Environment Ready"

if (-not $FrontendOnly) {
    Write-Host "  API:      " -NoNewline; Write-Host "http://localhost:$apiPort" -ForegroundColor Green
    if ($SwaggerPath) {
        Write-Host "  Swagger:  " -NoNewline; Write-Host "http://localhost:$apiPort$SwaggerPath" -ForegroundColor DarkGray
    }
    if ($HealthPath) {
        Write-Host "  Health:   " -NoNewline; Write-Host "http://localhost:$apiPort$HealthPath" -ForegroundColor DarkGray
    }
    if ($LogDir) {
        Write-Host "  Logs:     " -NoNewline; Write-Host "$BackendPath/$LogDir/" -ForegroundColor DarkGray
    }
}
if (-not $BackendOnly) {
    Write-Host "  Frontend: " -NoNewline; Write-Host "http://localhost:$frontendPort" -ForegroundColor Green
}
if (-not $SkipDocker -and -not $FrontendOnly -and $DockerMode -ne "none" -and $DockerContainerName) {
    $dbPort = docker port $DockerContainerName 3306 2>$null
    if ($dbPort) {
        $dbPort = $dbPort -replace '0\.0\.0\.0:', ''
        Write-Host "  Database: " -NoNewline; Write-Host "localhost:$dbPort" -ForegroundColor DarkGray
    }
}

Write-Host ""
Write-Host "  Press Ctrl+C to stop all services." -ForegroundColor Yellow
Write-Host "  Output is streamed below with prefixes [API] and [WEB]." -ForegroundColor DarkGray
Write-Host ""

# ═══════════════════════════════════════════════════════════════════════════
#  Stream logs
# ═══════════════════════════════════════════════════════════════════════════

try {
    while ($true) {
        foreach ($j in $jobs) {
            if ($j -and (Get-Job -Id $j.Id -ErrorAction SilentlyContinue)) {
                $output = Receive-Job -Id $j.Id -ErrorAction SilentlyContinue
                if ($output) {
                    $prefix = if ($j.Name -eq "Backend-API") { "[API]" } else { "[WEB]" }
                    $color  = if ($j.Name -eq "Backend-API") { "Magenta" } else { "Blue" }
                    foreach ($line in $output) {
                        $text = $line.ToString()
                        if ($text.Trim()) {
                            Write-Host "$prefix " -ForegroundColor $color -NoNewline
                            Write-Host $text
                        }
                    }
                }

                if ($j.State -eq "Completed" -or $j.State -eq "Failed") {
                    Write-Err "$($j.Name) has stopped (state: $($j.State))."
                }
            }
        }
        Start-Sleep -Milliseconds 500
    }
}
finally {
    Cleanup
}
