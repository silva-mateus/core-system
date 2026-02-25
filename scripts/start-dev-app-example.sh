#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════
#  Example wrapper for consuming projects (Linux / macOS).
#
#  Copy this file to your project root as start-dev.sh, make it executable
#  (chmod +x start-dev.sh), and adjust the variables below.
#
#  Usage:
#    ./start-dev.sh
#    ./start-dev.sh --skip-docker
#    ./start-dev.sh --backend-only
#    ./start-dev.sh --frontend-only
#
#  Your project structure should look like:
#
#    my-project/
#    ├── core/                  # git submodule (core-system)
#    ├── backend/               # .NET API
#    │   └── MyProject.Api.csproj
#    ├── frontend/              # Next.js or Vite app
#    │   └── package.json
#    ├── docker-compose.yml     # (optional)
#    ├── .env                   # (optional, auto-loaded)
#    └── start-dev.sh           # ← this wrapper
# ═══════════════════════════════════════════════════════════════════════════

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Project configuration ────────────────────────────────────────────────

export PROJECT_NAME="MyProject"
export PROJECT_ROOT="$SCRIPT_DIR"

export BACKEND_PATH="backend"
export BACKEND_CSPROJ="MyProject.Api.csproj"
export API_PORT_DEFAULT="5000"
export SWAGGER_PATH="/swagger"
export HEALTH_PATH="/api/health"
# export LOG_DIR="logs"

export FRONTEND_PATH="frontend"
export FRONTEND_RUNNER="next"          # "next" or "vite"
export FE_PORT_DEFAULT="3000"

export DOCKER_MODE="external"          # "external", "compose", or "none"
export DOCKER_CONTAINER_NAME="homelab-mysql"
# export DOCKER_COMPOSE_FILE="docker/docker-compose.yml"
# export DOCKER_COMPOSE_SERVICES="mysql"

# ── Run the core engine ──────────────────────────────────────────────────

exec "$SCRIPT_DIR/core/scripts/start-dev.sh" "$@"

# ═══════════════════════════════════════════════════════════════════════════
#  PARAMETER REFERENCE
# ═══════════════════════════════════════════════════════════════════════════
#
#  PROJECT_NAME        (required)  Display name, also used to find orphaned
#                                  dotnet processes.
#
#  PROJECT_ROOT        (required)  Always set to $SCRIPT_DIR.
#
#  BACKEND_PATH        (default: "backend")
#                      Relative path from project root to the folder that
#                      contains the .csproj file.
#                      Examples: "backend", "backend/src/MyApp.Api"
#
#  BACKEND_CSPROJ      (required)  The .csproj filename (not the full path).
#
#  FRONTEND_PATH       (default: "frontend")
#                      Relative path to the folder with package.json.
#
#  FRONTEND_RUNNER     (default: "next")
#                      "next" → sets PORT env var
#                      "vite" → sets VITE_DEV_PORT env var
#
#  API_PORT_DEFAULT    (default: "5000")   Overridable via API_PORT env var.
#  FE_PORT_DEFAULT     (default: "3000")   Overridable via FRONTEND_PORT env var.
#
#  SWAGGER_PATH        (default: "/swagger")   Set to "" to hide from summary.
#  HEALTH_PATH         (default: "")           e.g. "/api/health", "/health"
#  LOG_DIR             (default: "")           e.g. "logs" (relative to backend)
#
#  DOCKER_MODE         (default: "none")
#                      "none"     → skip Docker entirely
#                      "external" → check existing container (requires DOCKER_CONTAINER_NAME)
#                      "compose"  → run docker compose up -d
#
#  DOCKER_CONTAINER_NAME   Container name for health check.
#  DOCKER_COMPOSE_FILE     Relative path to docker-compose file.
#  DOCKER_COMPOSE_SERVICES Space-separated services to start, e.g. "mysql redis"
#
#  CLI flags: --skip-docker, --backend-only, --frontend-only
#
# ═══════════════════════════════════════════════════════════════════════════
#  MORE EXAMPLES
# ═══════════════════════════════════════════════════════════════════════════
#
#  Vite project with docker-compose:
#
#    export PROJECT_NAME="GestaoFinanceira"
#    export BACKEND_PATH="backend/src/GestaoFinanceira.Api"
#    export BACKEND_CSPROJ="GestaoFinanceira.Api.csproj"
#    export FRONTEND_RUNNER="vite"
#    export FE_PORT_DEFAULT="5173"
#    export SWAGGER_PATH="/openapi/v1.json"
#    export HEALTH_PATH="/health"
#    export LOG_DIR="logs"
#    export DOCKER_MODE="compose"
#    export DOCKER_COMPOSE_FILE="docker/docker-compose.yml"
#    export DOCKER_COMPOSE_SERVICES="mysql"
#    export DOCKER_CONTAINER_NAME="gestao-mysql"
#    export Serilog__MinimumLevel__Default="Debug"
#    export LOG_TO_FILE="true"
#    export LOG_RETENTION_DAYS="30"
#
#  No Docker at all:
#
#    export DOCKER_MODE="none"
