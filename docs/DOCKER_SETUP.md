# QueueSense — Docker Infrastructure & Setup Guide

---

## 1. Overview & Architecture

QueueSense is containerized as a decoupled, multi-container architecture using Docker Compose. The environment consists of three primary services running in an isolated bridge network:

```
[ Browser / Client ]
         │
         ├─── HTTP :3000 ──────────► [ queuesense-frontend ] (Nginx + React SPA)
         │                                   │
         │                                   ├── Reverse Proxy /api/ ───┐
         │                                   └── Reverse Proxy /hubs/ ──┤ (WebSockets)
         │                                                              ▼
         └─── HTTP :5000 ────────────────────────────────────────► [ queuesense-backend ] (ASP.NET Core .NET 9)
                                                                        │
                                                                        └── TCP :5432 ──► [ queuesense-postgres ] (PostgreSQL 17)
                                                                                                 │
                                                                                              [ queuesense_postgres_data ] (Named Volume)
```

### Container Topology
| Service | Container Name | Image / Base | Internal Port | Host Port | Purpose |
|---|---|---|---|---|---|
| `postgres` | `jobflow-postgres` | `postgres:17-alpine` | `5432` | `127.0.0.1:5432` | Primary transactional store, queue state, logs, AI results. |
| `backend` | `jobflow-backend` | `mcr.microsoft.com/dotnet/aspnet:9.0` | `5000` | `5000` | REST API, SignalR Hub, 4 `BackgroundService` execution workers. |
| `frontend` | `jobflow-frontend` | `nginx:alpine` | `80` | `3000` | Static asset hosting, SPA routing, API & WebSocket reverse proxy. |

---

## 2. Multi-Stage Dockerfile Specifications

### 2.1 Backend Dockerfile (`src/backend/Dockerfile`)
The backend uses a 2-stage build to produce a hardened, minimal production container:
1. **Build Stage (`mcr.microsoft.com/dotnet/sdk:9.0`)**:
   - Restores dependencies using solution and `.csproj` layer caching to maximize Docker cache hits.
   - Compiles and publishes `JobFlow.Api` in `Release` mode (`/p:UseAppHost=false`).
2. **Runtime Stage (`mcr.microsoft.com/dotnet/aspnet:9.0`)**:
   - Copies published binaries only (~120 MB total image size).
   - Enforces non-root security by executing as `USER $APP_UID` (UID 1654).
   - Exposes port `5000` via `ASPNETCORE_URLS=http://+:5000`.
   - Starts both REST controllers and all 4 background workers (`JobExecutionWorker`, `JobSchedulerWorker`, `JobMonitoringWorker`, `AIAnalysisWorker`) in a single process.

### 2.2 Frontend Dockerfile (`src/frontend/Dockerfile`)
The frontend uses a 2-stage build:
1. **Build Stage (`node:20-alpine`)**:
   - Installs dependencies deterministically via `npm ci` using `package-lock.json`.
   - Compiles TypeScript and builds production bundles via Vite (`npm run build`).
2. **Runtime Stage (`nginx:alpine`)**:
   - Copies static assets into `/usr/share/nginx/html`.
   - Injects custom `nginx.conf` providing SPA client-side route fallback (`try_files $uri $uri/ /index.html`).
   - Reverse proxies `/api/` and `/hubs/` (SignalR WebSocket handshake) to `http://backend:5000`.
   - Implements `/healthz` endpoint for orchestration health probes.

---

## 3. Orchestration & Networking

### Docker Compose Configuration (`docker-compose.yml`)
- **Network**: Dedicated bridge network `queuesense-network` for internal communication.
- **Service Discovery**: The backend connects to PostgreSQL using the DNS hostname `Host=postgres;Port=5432`.
- **Startup Order**:
  - `postgres` initializes and runs `pg_isready` health check.
  - `backend` starts only after `postgres` reports `service_healthy`.
  - `frontend` starts after `backend` is active.
- **Local Overrides (`docker-compose.override.yml`)**:
  - Automatically read by `docker compose`.
  - Sets `ASPNETCORE_ENVIRONMENT=Development` and binds PostgreSQL to `127.0.0.1:5432` for direct IDE/DBeaver access.

---

## 4. Management Commands (Makefile & PowerShell)

| Task | Make Command (Linux / macOS) | PowerShell Equivalent (Windows) | Description |
|---|---|---|---|
| **Start Stack** | `make up` | `docker compose up -d` | Starts all 3 containers in background |
| **Stop Stack** | `make down` | `docker compose down` | Stops containers while preserving named volume |
| **Build Images** | `make build` | `docker compose build` | Builds frontend and backend images |
| **Rebuild & Restart** | `make rebuild` | `docker compose up -d --build` | Full rebuild and restart with latest code |
| **Restart Services** | `make restart` | `docker compose restart` | Soft-restarts existing running containers |
| **Stream Logs** | `make logs` | `docker compose logs -f` | Streams consolidated logs from all services |
| **Container Status** | `make ps` | `docker compose ps` | Displays active container states and ports |
| **Database Shell** | `make db` | `docker compose exec postgres psql -U postgres -d jobflow` | Connects directly to interactive PostgreSQL CLI |
| **Apply Migrations** | `make migrate` | `dotnet ef database update --project src/backend/JobFlow.Infrastructure --startup-project src/backend/JobFlow.Api` | Executes pending EF Core migrations |
| **Health Check** | `make health` | `docker compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Health}}"` | Checks container health check statuses |
| **Help Menu** | `make help` | `Get-Content Makefile` | Lists all available make targets |

---

## 5. EF Core Migration Strategy

QueueSense uses EF Core code-first migrations with automatic schema synchronization:
1. **Automatic Startup Migration**:
   On backend container boot, [`Program.cs`](file:///D:/Projects/Job%20Monitoring%20Platform/src/backend/JobFlow.Api/Program.cs#L170) executes:
   ```csharp
   db.Database.Migrate();
   ```
   This automatically applies all pending migrations from [`JobFlow.Infrastructure/Migrations`](file:///D:/Projects/Job%20Monitoring%20Platform/src/backend/JobFlow.Infrastructure/Migrations) against PostgreSQL and initializes seed records idempotently.
2. **Manual Migration Execution**:
   To apply migrations from the developer machine against Docker PostgreSQL:
   ```powershell
   dotnet ef database update --project src/backend/JobFlow.Infrastructure --startup-project src/backend/JobFlow.Api
   ```

---

## 6. Security & Environment Configuration

1. **Environment File**: Copy `.env.example` to `.env`:
   ```powershell
   Copy-Item .env.example .env
   ```
2. **Credentials Isolation**:
   - Database passwords and JWT secrets are supplied via environment variables (`.env`).
   - `.dockerignore` prevents accidental inclusion of `.env` files, build caches (`bin/`, `obj/`, `node_modules/`), and sensitive credentials into image layers.
3. **Localhost Binding**:
   - Port 5432 is explicitly bound to `127.0.0.1:5432:5432` to prevent exposing the database over public network interfaces.
4. **Non-Root Execution**:
   - The backend runs as unprivileged user `$APP_UID` in compliance with container security best practices.

---

## 7. Troubleshooting & Host Virtualization

If Docker Desktop fails to start with `supportsVirtualization: false` or `500 Internal Server Error`:
1. **BIOS / UEFI**: Reboot and press `F2` (or `Fn+F2` / `Enter` on Lenovo PCs). Under `Security` or `Advanced CPU Configuration`, set **Intel (VMX) Virtualization Technology** to **Enabled**.
2. **WSL 2 Installation**: Open PowerShell as Administrator and run:
   ```powershell
   wsl --install --no-distribution
   dism.exe /online /enable-feature /featurename:VirtualMachinePlatform /all /norestart
   dism.exe /online /enable-feature /featurename:Microsoft-Windows-Subsystem-Linux /all /norestart
   ```
3. Restart Windows, open Docker Desktop, and verify engine readiness with `docker info`.
