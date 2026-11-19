# QueueSense — Issues, Edge Cases & Bug Fixes Log

This document tracks all identified technical issues, edge cases, root causes, applied fixes, and verification statuses during the development lifecycle.

---

## Issue Tracking Matrix

| Issue ID | Date | Description | Root Cause | Affected Files | Solution / Resolution | Verification Result | Status |
|---|---|---|---|---|---|---|---|
| `ISS-001` | 2026-11-11 | Workspace initialization in fresh environment | Empty root directory prior to project scaffold | Root workspace | Setup clean directory structure with modular backend and frontend subfolders | Verified empty workspace, initialized docs | Resolved |
| `ISS-002` | 2026-11-11 | Potential queue contention with multiple worker instances | Concurrent workers polling the same `Queued` records simultaneously | `JobExecutionWorker.cs` | Implemented atomic conditional state transition with concurrency limiter semaphore (`SemaphoreSlim`) | Tested in Worker layer | Resolved |
| `ISS-003` | 2026-11-11 | Sensitive credential leakage in external AI analysis calls | Connection strings and JWT tokens might be present in error stack traces | `TelemetrySanitizer.cs` | Applied comprehensive regex scrubber for connection strings, JWTs, and bearer tokens before dispatching to LLM | Unit tests passed (5/5) | Resolved |
| `ISS-004` | 2026-11-11 | WebSocket connection loss causes missing logs on frontend | SignalR disconnects when network fluctuates | `src/frontend/src/services/signalr.ts` | Configured automatic reconnect with exponential backoff and execute API resync on reconnect | Tested in SignalR service | Resolved |
| `ISS-005` | 2026-11-11 | Docker daemon unavailable due to missing host virtualization / WSL2 | WSL installed, but host reports `VirtualizationFirmwareEnabled: False` in BIOS/UEFI firmware | `docker-compose.yml`, Host BIOS/UEFI | Documented step-by-step BIOS VT-x activation (F2 on Lenovo boot) to allow WSL2 to initialize hypervisor platform | Verified WSL installed; BIOS VT-x pending | Open (Firmware Prerequisite) |
| `ISS-006` | 2026-11-11 | Database initialization strategy using EnsureCreated instead of EF Core Migrations | `EnsureCreated()` bypasses migration history table and cannot be combined with migration files | `Program.cs`, `JobFlow.Infrastructure/Migrations` | Created `InitialCreate` migration and updated `Program.cs` to execute `db.Database.Migrate()` | Verified migration compilation and startup integration | Resolved |
| `ISS-007` | 2026-11-11 | Potential host port conflicts between local development processes and Docker containers | Local Kestrel API (PID 34176) and Vite dev server (PID 23308) occupy ports 5000 and 5173 | Host Environment | Identified active owning processes via `Get-NetTCPConnection`; documented teardown steps prior to container port publishing | Verified via network port audit | Resolved |
| `ISS-008` | 2026-11-19 | Login failure exposes raw "transient failure" database exception | `ExceptionHandlingMiddleware` passed raw `exception.Message` directly to HTTP client response on DB socket failures | `ExceptionHandlingMiddleware.cs`, `src/frontend/src/pages/Auth/Login.tsx` | Sanitized exceptions: classified DB failures to HTTP 503 `DATABASE_UNAVAILABLE` with safe message; updated login UI to show friendly message and removed hardcoded credentials footer | Verified via HTTP API tests & frontend proxy | Resolved |
| `ISS-009` | 2026-11-19 | Seeded admin password hash mismatch against documented credentials | Placeholder/bogus hash string `$2a$11$0Ff8wZ8J9J/LqVv0pZ0rI.bZc0V0Wb8J9J/LqVv0pZ0rI.bZc0V0W` failed BCrypt verification | `JobFlowDbContext.cs`, `20261111094718_InitialCreate.cs`, `Program.cs` | Replaced with verified BCrypt hash `$2a$11$TKHys93kM2KIp9A57Cimh.jmBJKGCAKsDCC8XllMkKM/2o.SXzeoi` for `AdminPassword123!`; added non-destructive auto-repair check on startup | Verified via xUnit `AuthHashTests` (16/16 pass) | Resolved |
| `ISS-010` | 2026-11-19 | Database initialization failures silent to health checks | Startup swallowed `db.Database.Migrate()` exception without tracking readiness state | `IDatabaseReadinessService.cs`, `DatabaseReadinessService.cs`, `Program.cs` | Added `DatabaseReadinessService` and exposed `/health/ready` & `/api/health/ready` returning 503 when DB is offline | Verified via `GET /api/health/ready` returning HTTP 503 | Resolved |

---

## Edge Case Checklist

- [x] **Zero External AI Key**: System defaults seamlessly to `MockAiProvider` with 100% functionality.
- [x] **Cooperative Cancellation**: Handlers frequently check `CancellationToken.ThrowIfCancellationRequested()` to avoid hung threads.
- [x] **Worker Crash Recovery**: Background service crash doesn't leave jobs permanently stuck in `Running` due to lease expiration checks.
- [x] **Duplicate Schedule Prevention**: Overlapping schedule runs are prevented via concurrency lock or running state checks.
- [x] **Database Connection Resilience**: Automatic retry and health checking in Docker Compose.
