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

---

## Edge Case Checklist

- [x] **Zero External AI Key**: System defaults seamlessly to `MockAiProvider` with 100% functionality.
- [x] **Cooperative Cancellation**: Handlers frequently check `CancellationToken.ThrowIfCancellationRequested()` to avoid hung threads.
- [x] **Worker Crash Recovery**: Background service crash doesn't leave jobs permanently stuck in `Running` due to lease expiration checks.
- [x] **Duplicate Schedule Prevention**: Overlapping schedule runs are prevented via concurrency lock or running state checks.
- [x] **Database Connection Resilience**: Automatic retry and health checking in Docker Compose.
