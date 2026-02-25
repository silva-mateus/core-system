<#
.SYNOPSIS
    Example wrapper for consuming projects.

.DESCRIPTION
    Copy this file to your project root as start-dev.ps1 and adjust the
    parameters below to match your project layout.

    Your project structure should look like:

        my-project/
        ├── core/                  # git submodule (core-system)
        ├── backend/               # .NET API
        │   └── MyProject.Api.csproj
        ├── frontend/              # Next.js or Vite app
        │   └── package.json
        ├── docker-compose.yml     # (optional)
        ├── .env                   # (optional, auto-loaded)
        └── start-dev.ps1          # ← this wrapper

.EXAMPLE
    .\start-dev.ps1
    .\start-dev.ps1 -SkipDocker
    .\start-dev.ps1 -BackendOnly
    .\start-dev.ps1 -FrontendOnly

.NOTES
    All parameters have sensible defaults. Override only what differs.
    Environment variables API_PORT, FRONTEND_PORT, and SKIP_DOCKER
    can also be set in a .env file at the project root.
#>

param(
    [switch]$SkipDocker,
    [switch]$BackendOnly,
    [switch]$FrontendOnly
)

& "$PSScriptRoot\core\scripts\start-dev.ps1" `
    -ProjectName        "MyProject" `
    -ProjectRoot        $PSScriptRoot `
    -BackendPath        "backend" `
    -BackendCsproj      "MyProject.Api.csproj" `
    -FrontendPath       "frontend" `
    -FrontendRunner     "next" `
    -ApiPortDefault     "5000" `
    -FePortDefault      "3000" `
    -SwaggerPath        "/swagger" `
    -HealthPath         "/api/health" `
    -DockerMode         "external" `
    -DockerContainerName "homelab-mysql" `
    -SkipDocker:$SkipDocker `
    -BackendOnly:$BackendOnly `
    -FrontendOnly:$FrontendOnly

# ─────────────────────────────────────────────────────────────────────────
#  PARAMETER REFERENCE
# ─────────────────────────────────────────────────────────────────────────
#
#  -ProjectName        (required)  Display name, also used to find orphaned
#                                  dotnet processes. Should match the .csproj
#                                  namespace prefix (e.g. "MusicasIgreja").
#
#  -ProjectRoot        (required)  Always pass $PSScriptRoot here.
#
#  -BackendPath        (default: "backend")
#                      Relative path from project root to the folder that
#                      contains the .csproj file.
#                      Examples: "backend", "backend\src\MyApp.Api"
#
#  -BackendCsproj      (required)  The .csproj filename (not the full path).
#
#  -FrontendPath       (default: "frontend")
#                      Relative path to the folder with package.json.
#
#  -FrontendRunner     (default: "next")
#                      "next" → sets PORT env var
#                      "vite" → sets VITE_DEV_PORT env var
#
#  -ApiPortDefault     (default: "5000")   Overridable via API_PORT env var.
#  -FePortDefault      (default: "3000")   Overridable via FRONTEND_PORT env var.
#
#  -SwaggerPath        (default: "/swagger")  Set to "" to hide from summary.
#  -HealthPath         (default: $null)       e.g. "/api/health", "/health"
#  -LogDir             (default: $null)       e.g. "logs" (relative to BackendPath)
#
#  -DockerMode         (default: "none")
#                      "none"     → skip Docker entirely
#                      "external" → check existing container (requires -DockerContainerName)
#                      "compose"  → run docker compose up -d
#
#  -DockerContainerName  Container name for health check (external/compose modes).
#  -DockerComposeFile    Relative path to docker-compose file (compose mode).
#  -DockerComposeServices  Array of services to start, e.g. @("mysql")
#
#  -BackendEnv         Hashtable of extra env vars for the backend job.
#                      Example: @{ "Serilog__MinimumLevel__Default" = "Debug" }
#
#  -FrontendEnv        Hashtable of extra env vars for the frontend job.
#                      Example: @{ "NEXT_PUBLIC_API_URL" = "/api" }
#
# ─────────────────────────────────────────────────────────────────────────
#  MORE EXAMPLES
# ─────────────────────────────────────────────────────────────────────────
#
#  Vite project with docker-compose:
#
#    & "$PSScriptRoot\core\scripts\start-dev.ps1" `
#        -ProjectName        "GestaoFinanceira" `
#        -ProjectRoot        $PSScriptRoot `
#        -BackendPath        "backend\src\GestaoFinanceira.Api" `
#        -BackendCsproj      "GestaoFinanceira.Api.csproj" `
#        -FrontendRunner     "vite" `
#        -FePortDefault      "5173" `
#        -SwaggerPath        "/openapi/v1.json" `
#        -HealthPath         "/health" `
#        -LogDir             "logs" `
#        -DockerMode         "compose" `
#        -DockerComposeFile  "docker\docker-compose.yml" `
#        -DockerComposeServices @("mysql") `
#        -DockerContainerName "gestao-mysql" `
#        -BackendEnv @{
#            "Serilog__MinimumLevel__Default" = "Debug"
#            "LOG_TO_FILE"                    = "true"
#            "LOG_RETENTION_DAYS"             = "30"
#        } `
#        -SkipDocker:$SkipDocker `
#        -BackendOnly:$BackendOnly `
#        -FrontendOnly:$FrontendOnly
#
#  No Docker at all:
#
#    & "$PSScriptRoot\core\scripts\start-dev.ps1" `
#        -ProjectName   "SimpleApp" `
#        -ProjectRoot   $PSScriptRoot `
#        -BackendCsproj "SimpleApp.Api.csproj" `
#        -DockerMode    "none" `
#        -SkipDocker:$SkipDocker `
#        -BackendOnly:$BackendOnly `
#        -FrontendOnly:$FrontendOnly
