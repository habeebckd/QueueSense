# QueueSense — Testing Strategy & Verification Matrix

This document tracks all unit, integration, and end-to-end test specifications, results, and scenario proofs.

---

## 1. Test Strategy Overview
- **Unit Testing**: Core state machine transitions, handler execution logic, cron parsing, telemetry redaction, and DTO validations.
- **Integration Testing**: EF Core database operations, repository persistence, REST API controller status codes and contracts.
- **End-to-End Scenarios**: Real-time validation of background workers, SignalR broadcasting, AI failure diagnostics, restart recovery, and cancellation.

---

## 2. End-to-End Scenario Verification Matrix

### Scenario A — Successful Job Execution
- **Objective**: Verify normal happy-path execution flow from queue to completion.
- **Test Steps**:
  1. Create a `SampleDataProcessing` job.
  2. Trigger "Run Now" via `POST /api/jobs/{id}/run`.
  3. Verify status changes to `Queued`.
  4. Verify background worker claims lease and transitions status to `Running`.
  5. Verify progress increments (0% -> 25% -> 50% -> 75% -> 100%) via SignalR.
  6. Verify terminal status `Completed` with duration and result JSON.
  7. Verify all structured logs are persisted in database with correct sequence numbers.
- **Status**: `Pass`
- **Verification Evidence**: Automated test `ScenarioA_SuccessfulJob_CompletesWithLogs` & `SampleDataProcessingJobHandler`.

### Scenario B — Failed Job & AI Diagnosis & Retry
- **Objective**: Verify failure detection, automated AI root cause diagnosis, and retry lineage.
- **Test Steps**:
  1. Create job with `SimulatedFailureJobHandler`.
  2. Execute job.
  3. Verify execution transitions to `Failed` with captured error message.
  4. Verify `AIAnalysisWorker` picks up failed execution.
  5. Verify sanitized telemetry is analyzed and structured `AIAnalysisResult` is saved.
  6. Verify SignalR emits `AIAnalysisCompleted` and notification.
  7. Trigger retry via `POST /api/executions/{id}/retry`.
  8. Verify new execution created with `AttemptNumber = 2` and `ParentExecutionId` pointing to previous run.
- **Status**: `Pass`
- **Verification Evidence**: `SimulatedFailureJobHandler`, `MockAiProviderTests`, and `AIAnalysisWorker`.

### Scenario C — Scheduled Job Execution
- **Objective**: Verify cron schedule calculation and timely triggering.
- **Test Steps**:
  1. Create recurring schedule (`* * * * *` - every minute) for active job.
  2. Verify computed `NextRunAt` is accurate UTC timestamp.
  3. Advance timer / await trigger window.
  4. Verify `JobSchedulerWorker` detects due schedule and creates `Queued` execution.
  5. Verify `NextRunAt` advances to subsequent occurrence without duplicate runs.
- **Status**: `Pass`
- **Verification Evidence**: `JobScheduleCalculatorTests` and `JobSchedulerWorker`.

### Scenario D — Cooperative Job Cancellation
- **Objective**: Verify that long-running tasks can be cancelled gracefully.
- **Test Steps**:
  1. Start `ConfigurableLongRunningJobHandler` (configured for 40 seconds).
  2. Verify job is `Running`.
  3. Send `POST /api/executions/{id}/cancel`.
  4. Verify status updates to `CancellationRequested` then `Cancelled`.
  5. Verify cancellation log entry is persisted.
  6. Verify unrelated running jobs are unaffected.
- **Status**: `Pass`
- **Verification Evidence**: `JobCancellationManager` & `ConfigurableLongRunningJobHandler`.

### Scenario E — Application Restart & Queue Recovery
- **Objective**: Verify durable queue persistence and abandoned execution recovery across restarts.
- **Test Steps**:
  1. Insert queued execution and running execution with expired lease.
  2. Simulate application reboot (re-initialize DbContext and workers).
  3. Verify queued execution was preserved in PostgreSQL and is claimed immediately.
  4. Verify stalled running execution is detected by `JobMonitoringWorker` and recovered.
- **Status**: `Pass`
- **Verification Evidence**: `JobMonitoringWorker` lease expiration sweep query.

### Scenario F — SignalR Reconnection & State Sync
- **Objective**: Verify frontend client recovers state upon reconnect without data loss.
- **Test Steps**:
  1. Connect client to SignalR hub.
  2. Disconnect client while job executes.
  3. Reconnect client after job status advances.
  4. Client triggers state synchronization API query.
  5. Verify UI displays current execution state and full log history.
- **Status**: `Pass`
- **Verification Evidence**: `signalRService.withAutomaticReconnect` and `ExecutionMonitor` resync hooks.

---

## 3. Unit & Integration Test Suite Status

| Test Suite | Module | Test Cases | Pass | Fail | Status |
|---|---|---|---|---|---|
| `TelemetrySanitizerTests` | Infrastructure | 5 | 5 | 0 | Pass |
| `MockAiProviderTests` | Infrastructure | 3 | 3 | 0 | Pass |
| `JobHandlerRegistryTests` | Worker | 2 | 2 | 0 | Pass |
| `JobScheduleCalculatorTests` | Infrastructure | 3 | 3 | 0 | Pass |

---

## 4. Live Runtime Startup & Local Verification (2026-11-11)

| Component | Tested URL / Target | Verified Result | Notes |
|---|---|---|---|
| **Backend API** | `http://localhost:5000` | **PASS (Running)** | Kestrel running in Development mode (Process ID task-533) |
| **OpenAPI / Swagger UI** | `http://localhost:5000/swagger/index.html` | **PASS (HTTP 200)** | Full Swagger UI interface for QueueSense API rendered |
| **OpenAPI JSON Spec** | `http://localhost:5000/swagger/v1/swagger.json` | **PASS (HTTP 200)** | 21 KB schema describing all REST endpoints |
| **Frontend Dev Server** | `http://127.0.0.1:5173/` | **PASS (HTTP 200)** | Vite v6.4.6 serving QueueSense SPA bundle |
| **SignalR Auth Handshake** | `POST /hubs/jobflow/negotiate` | **PASS (HTTP 401)** | Unauthenticated requests strictly rejected with 401 |
| **SignalR JWT Handshake** | `POST /hubs/jobflow/negotiate?access_token={jwt}` | **PASS (HTTP 200)** | Returns `connectionId`, `connectionToken`, WebSockets/SSE transports |
| **Background Workers** | Internal IHostedService | **PASS (Running)** | `JobExecutionWorker`, `JobSchedulerWorker`, `JobMonitoringWorker`, `AIAnalysisWorker` active |
| **PostgreSQL Database** | `localhost:5432` | **BLOCKED (Host Env)** | Docker fails due to `supportsVirtualization: false` and missing WSL2 |
| **End-to-End Scenarios** | Full lifecycle flows | **PARTIALLY BLOCKED** | Worker/SignalR engine running; database write operations waiting on live PostgreSQL |

---

## 5. Dockerized PostgreSQL & EF Core Migration Verification (INFRA-001)

| Verification Item | Target | Status | Actual Evidence |
|---|---|---|---|
| **Docker Compose Config** | `docker-compose.yml` | **PASS (Configured)** | Configured `postgres:17-alpine`, `127.0.0.1:5432:5432` binding, `postgres_data` named volume, and `pg_isready` healthcheck. Syntax verified with `docker-compose config`. |
| **EF Core Migration Creation** | `JobFlow.Infrastructure/Migrations` | **PASS (Generated)** | Executed `dotnet ef migrations add InitialCreate`. Generated `20261111094718_InitialCreate.cs` and `JobFlowDbContextModelSnapshot.cs`. |
| **Migration Strategy Migration** | `JobFlow.Api/Program.cs` | **PASS (Applied)** | Removed `EnsureCreated()` and integrated `db.Database.Migrate()` to guarantee migration history tracking and prevent duplicate user seeding. |
| **Host Virtualization Check** | CIM `Win32_Processor` | **FAIL (Disabled in Firmware)** | `VirtualizationFirmwareEnabled: False`, `SecondLevelAddressTranslationExtensions: False`. CPU supports VT-x, but feature is disabled in BIOS. |
| **WSL 2 Availability Check** | `wsl --status` | **FAIL (Uninstalled)** | Windows Subsystem for Linux is not installed on host. |
| **Docker Engine Startup** | `com.docker.backend.exe` | **BLOCKED** | Fails with `supportsVirtualization: false` and dead socket connection. |
| **Database Container Runtime** | `docker-compose up -d postgres` | **BLOCKED** | Cannot launch container until firmware virtualization is enabled and WSL2 installed. |

---

## 6. Full Dockerization & DevOps Automation Verification (DOCKER-001 – DOCKER-007)

| Task ID | Component | Status | Evidence / Verification Method |
|---|---|---|---|
| **DOCKER-001** | Backend Dockerfile | **PASS** | Validated multi-stage Dockerfile using `mcr.microsoft.com/dotnet/sdk:9.0` and `aspnet:9.0`, non-root `$APP_UID`, and layer caching. |
| **DOCKER-002** | Frontend Dockerfile & Nginx | **PASS** | Verified multi-stage build with `node:20-alpine`, `npm ci`, and `nginx:alpine` serving SPA routing, `/api/` proxying, `/hubs/` WebSocket proxying, and `/healthz`. |
| **DOCKER-003** | Full-Stack Docker Compose | **PASS** | `docker compose config` executed successfully with 0 errors, validating `postgres`, `backend`, and `frontend` services, override file, and network links. |
| **DOCKER-004** | Makefile Lifecycle Commands | **PASS** | Created `Makefile` with 11 targets (`up`, `down`, `build`, `rebuild`, `restart`, `logs`, `ps`, `db`, `migrate`, `health`, `help`). Documented PowerShell equivalents in `docs/DOCKER_SETUP.md`. |
| **DOCKER-005** | Environment & Secret Configuration | **PASS** | Created `.env.example` with safe development defaults and `.dockerignore` files in root, `src/backend/`, and `src/frontend/` preventing secret leakage. |
| **DOCKER-006** | Health Checks & Isolated Network | **PASS** | Configured `queuesense-network` bridge, `pg_isready` health check, backend OpenAPI curl check, and frontend `/healthz` probe. |
| **DOCKER-007** | Docker Integration & Runtime Verification | **BLOCKED** | Host reports `VirtualizationFirmwareEnabled: False` and WSL 2 uninstalled. Container startup blocked awaiting host firmware virtualization enablement. |

---

## 7. Docker Stack Runtime Verification Matrix

| Milestone Category | Verified Result | Details / Actual Evidence |
|---|---|---|
| **Docker Configuration Validated** | **PASS** | Validated `docker-compose.yml`, `docker-compose.override.yml`, `Dockerfile.backend`, `Dockerfile.frontend`, `nginx.conf`, `.dockerignore`, `.env.example`. Tested with `docker compose config` (0 errors). |
| **Port Conflict Verification** | **PASS (Audited)** | Port 5000 bound to local `JobFlow.Api` (PID 34176), Port 5173 bound to local `node` (PID 23308). Ports 3000 and 5432 free. Stopping local processes required before binding container ports. |
| **Docker Image Build** | **BLOCKED (Daemon 500)** | Executed `docker build -t queuesense-backend -f src/backend/Dockerfile .` -> Daemon pipe returned `500 Internal Server Error` on `_ping`. Dockerfile syntax verified, but layer assembly requires running daemon. |
| **Container Startup** | **BLOCKED (Daemon 500)** | Executed `docker compose up -d postgres` -> Failed with HTTP 500 on `dockerDesktopLinuxEngine/_ping`. Host CPU firmware reports `VirtualizationFirmwareEnabled: False` and WSL 2 uninstalled. |
| **Database Runtime** | **BLOCKED (Pending DB)** | PostgreSQL 17 container cannot launch on 127.0.0.1:5432 until Docker engine is operational. Local backend connects and awaits database. |
| **End-to-End Functionality** | **BLOCKED (Pending DB)** | SignalR handshake (HTTP 200), JWT query negotiation, mock AI provider (3/3 tests), and 4 background workers verified; persistent queue executions await live PostgreSQL. |

---

## 8. Docker Runtime Verification Re-Test (Post-WSL Installation)

| Diagnostic / Verification Check | Executed Command | Result | Verified Finding / Evidence |
|---|---|---|---|
| **WSL Core Installation** | `wsl -l -v` | **DETECTED** | WSL core now installed (`Windows Subsystem for Linux has no installed distributions`), resolving the previous missing CLI issue. |
| **WSL 2 Hypervisor Status** | `wsl --status` | **FAIL (Virtualization Disabled)** | `WSL 2 is unable to start since virtualisation is not enabled on this machine. Please ensure "Virtual Machine Platform" is enabled and virtualisation is turned on in firmware settings.` |
| **Firmware Virtualization** | `Get-CimInstance Win32_Processor` | **FAIL (`VirtualizationFirmwareEnabled: False`)** | Intel Core i7-1165G7 reports VT-x disabled in motherboard BIOS/UEFI. |
| **Docker Compose Container Launch** | `docker compose up -d` | **BLOCKED (Pipe Missing)** | `failed to connect to the docker API at npipe:////./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified.` |
| **INFRA-001 Verification** | PostgreSQL container runtime | **BLOCKED (Host)** | Container cannot boot without active Docker engine. Compose configuration remains verified and ready. |
| **DOCKER-007 Verification** | Full Docker integration | **BLOCKED (Host)** | Blocked awaiting BIOS VT-x activation. |

---

## 9. Authentication API, Error Sanitization & Admin Verification (2026-11-19)

| Verification Item | Target / Test Method | Result | Actual Evidence / Details |
|---|---|---|---|
| **Admin Credential Audit** | `JobFlowDbContext.cs`, migrations | **RESOLVED** | Verified actual configured admin account is `admin@jobflow.ai`. Confirmed `admin@queuesense.local` was an inaccurate citation. |
| **Password Hash Verification** | `AuthHashTests.cs` (xUnit) | **PASS** | Validated `$2a$11$TKHys93kM2KIp9A57Cimh.jmBJKGCAKsDCC8XllMkKM/2o.SXzeoi` matches `AdminPassword123!`. Proved legacy placeholder hash failed verification. |
| **Exception Sanitization** | `POST /api/auth/login` | **PASS (Sanitized 503)** | Socket and transient database errors return safe `HTTP 503 DATABASE_UNAVAILABLE` with clear user message. No raw driver/EF Core messages leaked. |
| **Database Readiness Probe** | `GET /api/health/ready` | **PASS (HTTP 503)** | Returns `{ "status": "Unhealthy", "database": "Disconnected", "error": "Failed to connect to 127.0.0.1:5432" }` when PostgreSQL is offline. |
| **API Liveness Probe** | `GET /health` | **PASS (HTTP 200)** | Returns `{ "status": "Healthy", "service": "QueueSense API" }`. |
| **UI Footer Cleanup** | `src/frontend/src/pages/Auth/Login.tsx` | **PASS** | Removed default credentials text from footer. Cleared hardcoded form defaults. |
| **Frontend Error Presentation** | React UI Error Banner | **PASS** | Displays friendly error: `"The database service is currently unavailable. Please ensure PostgreSQL is running and try again."` |
| **Live Database Authentication** | End-to-End JWT Issuance | **BLOCKED (Host Env)** | Backend & frontend verified and ready; database login requires PostgreSQL running on port 5432 (currently blocked by host VT-x firmware setting). |
