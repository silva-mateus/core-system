#!/usr/bin/env bash
# ═══════════════════════════════════════════════════════════════════════════
#  Generic dev environment starter for core-system projects (Linux / macOS)
#
#  Launches Docker containers (if configured), the .NET backend API, and the
#  frontend dev server (Next.js or Vite). All output is streamed into a single
#  terminal with colored prefixes [API] and [WEB].
#
#  Press Ctrl+C to stop all services.
#
#  This script is meant to be called from a thin wrapper in each consuming
#  project -- do not run it directly.
#
#  Location : core-system/scripts/start-dev.sh
#  Called by: <project>/start-dev.sh  (thin wrapper, ~20 lines)
# ═══════════════════════════════════════════════════════════════════════════

set -euo pipefail

# ── Defaults (overridden by caller or .env) ──────────────────────────────

PROJECT_NAME="${PROJECT_NAME:?PROJECT_NAME is required}"
PROJECT_ROOT="${PROJECT_ROOT:?PROJECT_ROOT is required}"

BACKEND_PATH="${BACKEND_PATH:-backend}"
BACKEND_CSPROJ="${BACKEND_CSPROJ:?BACKEND_CSPROJ is required}"
API_PORT_DEFAULT="${API_PORT_DEFAULT:-5000}"
SWAGGER_PATH="${SWAGGER_PATH:-/swagger}"
HEALTH_PATH="${HEALTH_PATH:-}"
LOG_DIR="${LOG_DIR:-}"

FRONTEND_PATH="${FRONTEND_PATH:-frontend}"
FRONTEND_RUNNER="${FRONTEND_RUNNER:-next}"
FE_PORT_DEFAULT="${FE_PORT_DEFAULT:-3000}"

DOCKER_MODE="${DOCKER_MODE:-none}"
DOCKER_CONTAINER_NAME="${DOCKER_CONTAINER_NAME:-}"
DOCKER_COMPOSE_FILE="${DOCKER_COMPOSE_FILE:-}"
DOCKER_COMPOSE_SERVICES="${DOCKER_COMPOSE_SERVICES:-}"

SKIP_DOCKER="${SKIP_DOCKER:-false}"
BACKEND_ONLY="${BACKEND_ONLY:-false}"
FRONTEND_ONLY="${FRONTEND_ONLY:-false}"

# ── Parse CLI flags ──────────────────────────────────────────────────────

for arg in "$@"; do
    case "$arg" in
        --skip-docker)   SKIP_DOCKER=true ;;
        --backend-only)  BACKEND_ONLY=true ;;
        --frontend-only) FRONTEND_ONLY=true ;;
    esac
done

# ── Colors & helpers ─────────────────────────────────────────────────────

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
BLUE='\033[0;34m'
GRAY='\033[0;90m'
NC='\033[0m'

header()  { echo -e "\n${CYAN}=== $1 ===${NC}"; }
ok()      { echo -e "  ${GREEN}[OK]${NC} $1"; }
info()    { echo -e "  ${YELLOW}[..]${NC} $1"; }
err()     { echo -e "  ${RED}[!!]${NC} $1"; }

# ── Load .env ────────────────────────────────────────────────────────────

ENV_FILE="$PROJECT_ROOT/.env"
if [ -f "$ENV_FILE" ]; then
    info "Loading .env from project root"
    set -a
    # shellcheck disable=SC1090
    source "$ENV_FILE"
    set +a
    ok ".env loaded."
fi

# ── Resolve ports ────────────────────────────────────────────────────────

API_PORT="${API_PORT:-$API_PORT_DEFAULT}"
FRONTEND_PORT="${FRONTEND_PORT:-$FE_PORT_DEFAULT}"

if [ "${SKIP_DOCKER_ENV:-}" = "true" ]; then
    SKIP_DOCKER=true
fi

# ── Track PIDs for cleanup ───────────────────────────────────────────────

BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
    header "Stopping services"

    if [ -n "$FRONTEND_PID" ] && kill -0 "$FRONTEND_PID" 2>/dev/null; then
        info "Stopping frontend (PID: $FRONTEND_PID)..."
        kill "$FRONTEND_PID" 2>/dev/null || true
        wait "$FRONTEND_PID" 2>/dev/null || true
    fi

    if [ -n "$BACKEND_PID" ] && kill -0 "$BACKEND_PID" 2>/dev/null; then
        info "Stopping backend (PID: $BACKEND_PID)..."
        kill "$BACKEND_PID" 2>/dev/null || true
        wait "$BACKEND_PID" 2>/dev/null || true
    fi

    # Kill any remaining dotnet processes for this project
    if command -v pgrep &>/dev/null; then
        pgrep -f "$PROJECT_NAME" | while read -r pid; do
            local name
            name=$(ps -p "$pid" -o comm= 2>/dev/null || echo "")
            if [ "$name" = "dotnet" ]; then
                info "Killing orphaned dotnet (PID: $pid)"
                kill "$pid" 2>/dev/null || true
            fi
        done
    fi

    ok "All services stopped."
    exit 0
}

trap cleanup SIGINT SIGTERM EXIT

# ── Kill stale processes ─────────────────────────────────────────────────

kill_stale_on_port() {
    local port="$1"
    local proc_name="$2"

    if command -v lsof &>/dev/null; then
        lsof -ti ":$port" 2>/dev/null | while read -r pid; do
            local name
            name=$(ps -p "$pid" -o comm= 2>/dev/null || echo "")
            if [ "$name" = "$proc_name" ]; then
                info "Killing stale $proc_name on port $port (PID: $pid)"
                kill -9 "$pid" 2>/dev/null || true
            fi
        done
    elif command -v ss &>/dev/null; then
        ss -tlnp "sport = :$port" 2>/dev/null | grep "$proc_name" | \
            grep -oP 'pid=\K[0-9]+' | while read -r pid; do
                info "Killing stale $proc_name on port $port (PID: $pid)"
                kill -9 "$pid" 2>/dev/null || true
            done
    fi
}

header "Checking for stale processes"
kill_stale_on_port "$API_PORT" "dotnet"
kill_stale_on_port "$FRONTEND_PORT" "node"
ok "Stale process check complete."

# ── Wait for container health ────────────────────────────────────────────

wait_container_healthy() {
    local container="$1"
    local timeout="${2:-60}"
    local waited=0

    while [ "$waited" -lt "$timeout" ]; do
        local health
        health=$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null || echo "")
        if [ "$health" = "healthy" ]; then return 0; fi
        if [ -z "$health" ]; then return 0; fi
        sleep 2
        waited=$((waited + 2))
        echo -ne "${GRAY}.${NC}"
    done
    echo ""
    return 1
}

# ── 1. Docker ────────────────────────────────────────────────────────────

if [ "$SKIP_DOCKER" != "true" ] && [ "$FRONTEND_ONLY" != "true" ] && [ "$DOCKER_MODE" != "none" ]; then
    header "Docker ($DOCKER_MODE)"

    if ! command -v docker &>/dev/null; then
        err "Docker not found in PATH. Install Docker or use --skip-docker."
        exit 1
    fi

    case "$DOCKER_MODE" in
        external)
            if [ -z "$DOCKER_CONTAINER_NAME" ]; then
                err "DOCKER_MODE 'external' requires DOCKER_CONTAINER_NAME."
                exit 1
            fi

            running=$(docker ps --filter "name=^${DOCKER_CONTAINER_NAME}$" --format '{{.Names}}' 2>/dev/null || echo "")
            if [ "$running" = "$DOCKER_CONTAINER_NAME" ]; then
                health=$(docker inspect --format='{{.State.Health.Status}}' "$DOCKER_CONTAINER_NAME" 2>/dev/null || echo "")
                if [ "$health" = "healthy" ]; then
                    ok "$DOCKER_CONTAINER_NAME is running and healthy."
                elif [ -n "$health" ]; then
                    info "$DOCKER_CONTAINER_NAME running (status: $health). Waiting..."
                    if ! wait_container_healthy "$DOCKER_CONTAINER_NAME"; then
                        err "$DOCKER_CONTAINER_NAME did not become healthy within 60s."
                        exit 1
                    fi
                    ok "$DOCKER_CONTAINER_NAME is ready."
                else
                    ok "$DOCKER_CONTAINER_NAME is running (no health check configured)."
                fi
            else
                err "Container '$DOCKER_CONTAINER_NAME' is not running."
                info "Start it manually before running this script."
                exit 1
            fi
            ;;

        compose)
            compose_cmd="docker compose"

            if [ -n "$DOCKER_COMPOSE_FILE" ]; then
                if [[ "$DOCKER_COMPOSE_FILE" = /* ]]; then
                    compose_cmd="$compose_cmd -f $DOCKER_COMPOSE_FILE"
                else
                    compose_cmd="$compose_cmd -f $PROJECT_ROOT/$DOCKER_COMPOSE_FILE"
                fi
            fi

            compose_cmd="$compose_cmd up -d"

            if [ -n "$DOCKER_COMPOSE_SERVICES" ]; then
                compose_cmd="$compose_cmd $DOCKER_COMPOSE_SERVICES"
            fi

            info "Running: $compose_cmd"
            (cd "$PROJECT_ROOT" && eval "$compose_cmd")

            if [ -n "$DOCKER_CONTAINER_NAME" ]; then
                info "Waiting for $DOCKER_CONTAINER_NAME to be healthy..."
                if ! wait_container_healthy "$DOCKER_CONTAINER_NAME"; then
                    err "$DOCKER_CONTAINER_NAME did not become healthy within 60s."
                    exit 1
                fi
                ok "$DOCKER_CONTAINER_NAME is ready."
            else
                ok "Docker Compose services started."
            fi
            ;;
    esac
fi

# ── 2. Backend (.NET API) ────────────────────────────────────────────────

if [ "$FRONTEND_ONLY" != "true" ]; then
    header "Backend API (.NET)"

    if [[ "$BACKEND_PATH" = /* ]]; then
        backend_full="$BACKEND_PATH"
    else
        backend_full="$PROJECT_ROOT/$BACKEND_PATH"
    fi

    csproj_path="$backend_full/$BACKEND_CSPROJ"
    if [ ! -f "$csproj_path" ]; then
        err "Backend project not found: $csproj_path"
        exit 1
    fi

    if [ -n "$LOG_DIR" ]; then
        mkdir -p "$backend_full/$LOG_DIR"
    fi

    info "Starting backend on http://localhost:$API_PORT ..."

    (
        cd "$backend_full"
        export ASPNETCORE_ENVIRONMENT=Development
        export ASPNETCORE_URLS="http://localhost:$API_PORT"
        export Cors__AllowedOrigins__0="http://localhost:$FRONTEND_PORT"
        dotnet run --no-launch-profile 2>&1 | while IFS= read -r line; do
            [ -n "$line" ] && echo -e "${MAGENTA}[API]${NC} $line"
        done
    ) &
    BACKEND_PID=$!

    sleep 3

    if ! kill -0 "$BACKEND_PID" 2>/dev/null; then
        err "Backend failed to start."
        exit 1
    fi
    ok "Backend started (PID: $BACKEND_PID)."
fi

# ── 3. Frontend (Next.js / Vite) ─────────────────────────────────────────

if [ "$BACKEND_ONLY" != "true" ]; then
    if [ "$FRONTEND_RUNNER" = "next" ]; then
        runner_label="Next.js"
    else
        runner_label="Vite"
    fi
    header "Frontend ($runner_label)"

    if [[ "$FRONTEND_PATH" = /* ]]; then
        frontend_full="$FRONTEND_PATH"
    else
        frontend_full="$PROJECT_ROOT/$FRONTEND_PATH"
    fi

    if [ ! -f "$frontend_full/package.json" ]; then
        err "Frontend not found: $frontend_full"
        exit 1
    fi

    if [ ! -d "$frontend_full/node_modules" ]; then
        info "Installing npm dependencies..."
        (cd "$frontend_full" && npm install)
    fi

    info "Starting frontend on http://localhost:$FRONTEND_PORT ..."

    (
        cd "$frontend_full"
        export API_PROXY_TARGET="http://localhost:$API_PORT"

        case "$FRONTEND_RUNNER" in
            next) export PORT="$FRONTEND_PORT" ;;
            vite) export VITE_DEV_PORT="$FRONTEND_PORT" ;;
        esac

        npm run dev 2>&1 | while IFS= read -r line; do
            [ -n "$line" ] && echo -e "${BLUE}[WEB]${NC} $line"
        done
    ) &
    FRONTEND_PID=$!

    sleep 2

    if ! kill -0 "$FRONTEND_PID" 2>/dev/null; then
        err "Frontend failed to start."
        exit 1
    fi
    ok "Frontend started (PID: $FRONTEND_PID)."
fi

# ── Summary ──────────────────────────────────────────────────────────────

header "$PROJECT_NAME - Development Environment Ready"

if [ "$FRONTEND_ONLY" != "true" ]; then
    echo -e "  API:      ${GREEN}http://localhost:$API_PORT${NC}"
    if [ -n "$SWAGGER_PATH" ]; then
        echo -e "  Swagger:  ${GRAY}http://localhost:$API_PORT$SWAGGER_PATH${NC}"
    fi
    if [ -n "$HEALTH_PATH" ]; then
        echo -e "  Health:   ${GRAY}http://localhost:$API_PORT$HEALTH_PATH${NC}"
    fi
    if [ -n "$LOG_DIR" ]; then
        echo -e "  Logs:     ${GRAY}$BACKEND_PATH/$LOG_DIR/${NC}"
    fi
fi

if [ "$BACKEND_ONLY" != "true" ]; then
    echo -e "  Frontend: ${GREEN}http://localhost:$FRONTEND_PORT${NC}"
fi

if [ "$SKIP_DOCKER" != "true" ] && [ "$FRONTEND_ONLY" != "true" ] && [ "$DOCKER_MODE" != "none" ] && [ -n "$DOCKER_CONTAINER_NAME" ]; then
    db_port=$(docker port "$DOCKER_CONTAINER_NAME" 3306 2>/dev/null | sed 's/0\.0\.0\.0://' || echo "")
    if [ -n "$db_port" ]; then
        echo -e "  Database: ${GRAY}localhost:$db_port${NC}"
    fi
fi

echo ""
echo -e "  ${YELLOW}Press Ctrl+C to stop all services.${NC}"
echo -e "  ${GRAY}Output is streamed above with prefixes [API] and [WEB].${NC}"
echo ""

# ── Wait for background processes ────────────────────────────────────────

wait
