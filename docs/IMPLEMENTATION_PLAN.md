# QueueSense — Implementation Plan

This plan organizes the complete development of QueueSense into sequential, verified phases according to the master prompt requirements.

---

## Phase 1 — Requirement Analysis & Architecture
- **Phase ID**: `PHASE-01`
- **Objective**: Establish comprehensive architecture, domain models, database schema, API contracts, workflow specifications, and tracking infrastructure.
- **Dependencies**: None.
- **Tasks**:
  - `P1-01`: Inspect environment and project structure.
  - `P1-02`: Define functional and non-functional requirements (`REQUIREMENTS.md`).
  - `P1-03`: Author high-level and modular architecture specifications (`ARCHITECTURE.md`).
  - `P1-04`: Create state machine and lifecycle workflows (`JOB_WORKFLOW.md`).
  - `P1-05`: Define REST API contracts and SignalR schemas (`API_CONTRACTS.md`, `SIGNALR_EVENTS.md`).
  - `P1-06`: Detail database entity schemas, relations, and indexes (`DATABASE_DESIGN.md`).
  - `P1-07`: Formulate AI integration and prompt sanitization design (`AI_INTEGRATION.md`).
  - `P1-08`: Establish testing framework and issue tracking structures (`TESTING.md`, `ISSUES_AND_FIXES.md`).
  - `P1-09`: Create project setup and environment guide (`SETUP_GUIDE.md`).
  - `P1-10`: Establish master progress tracker (`PROGRESS_TRACKER.md`).
- **Expected Deliverables**: All 12 mandatory documents inside `docs/`.
- **Acceptance Criteria**: Comprehensive architectural and technical alignment across all subsystems.
- **Testing Requirements**: Document review and schema validation.
- **Current Status**: In Progress.

---

## Phase 2 — Backend Foundation
- **Phase ID**: `PHASE-02`
- **Objective**: Build the .NET Web API solution, domain entities, EF Core DbContext, PostgreSQL migrations, JWT authentication, and Job CRUD APIs.
- **Dependencies**: `PHASE-01`.
- **Tasks**:
  - `P2-01`: Setup .NET Solution (`JobFlow.sln`, `JobFlow.Core`, `JobFlow.Infrastructure`, `JobFlow.Worker`, `JobFlow.Api`, `JobFlow.Tests`).
  - `P2-02`: Implement core domain entities, enums, and state models in `JobFlow.Core`.
  - `P2-03`: Implement `JobFlowDbContext` with EF Core fluent configurations, relationships, and indices.
  - `P2-04`: Create initial PostgreSQL database migration and seeding scripts.
  - `P2-05`: Implement JWT authentication service, password hashing, and user management.
  - `P2-06`: Implement Job Management REST controllers with pagination, filtering, and validation (`/api/jobs`).
  - `P2-07`: Implement Schedule Management REST controllers (`/api/schedules`).
  - `P2-08`: Configure Serilog structured logging and global exception handling middleware.
- **Expected Deliverables**: Compiling .NET backend solution with database context and operational CRUD APIs.
- **Acceptance Criteria**: JWT issuance, Job CRUD, Schedule CRUD operational with proper validation and status codes.
- **Testing Requirements**: Unit tests for models and auth; API endpoint integration testing.
- **Current Status**: Pending.

---

## Phase 3 — Background Job Engine
- **Phase ID**: `PHASE-03`
- **Objective**: Develop the durable queue, worker execution loop, job handler registry, scheduler, cancellation, and crash recovery.
- **Dependencies**: `PHASE-02`.
- **Tasks**:
  - `P3-01`: Define `IJobHandler`, execution context, and `JobHandlerRegistry`.
  - `P3-02`: Implement five built-in job handlers:
    1. `SampleDataProcessingJobHandler`
    2. `BatchRecordProcessingJobHandler`
    3. `ReportGenerationJobHandler`
    4. `SimulatedFailureJobHandler`
    5. `ConfigurableLongRunningJobHandler`
  - `P3-03`: Implement `JobExecutionWorker` with safe database queue polling and worker lease acquisition.
  - `P3-04`: Implement `JobSchedulerWorker` with Cron and interval calculation.
  - `P3-05`: Implement `JobMonitoringWorker` for crash detection and lease-expired recovery.
  - `P3-06`: Implement retry mechanism linking new attempts to previous execution records.
  - `P3-07`: Implement cooperative cancellation via `CancellationTokenSource`.
  - `P3-08`: Implement Execution REST controllers (`/api/executions/{id}`, `/logs`, `/cancel`, `/retry`).
- **Expected Deliverables**: Fully functional asynchronous background job processing engine.
- **Acceptance Criteria**: Jobs transition through states correctly; progress is tracked; cancellations and retries work.
- **Testing Requirements**: Handler execution tests, concurrency claiming tests, crash recovery tests.
- **Current Status**: Pending.

---

## Phase 4 — Real-Time SignalR Observability
- **Phase ID**: `PHASE-04`
- **Objective**: Implement bidirectional real-time communications for job execution updates, progress, logs, and alerts.
- **Dependencies**: `PHASE-03`.
- **Tasks**:
  - `P4-01`: Create `IJobFlowHubClient` strongly-typed interface and `JobFlowHub`.
  - `P4-02`: Configure SignalR authentication via JWT query token.
  - `P4-03`: Implement `SignalRNotificationDispatcher` with buffered log streaming.
  - `P4-04`: Emit typed events: `JobQueued`, `JobStarted`, `JobProgressUpdated`, `JobLogAdded`, `JobCompleted`, `JobFailed`, `JobCancelled`, `JobTimedOut`.
  - `P4-05`: Build client-side SignalR connection manager with auto-reconnection and state sync.
- **Expected Deliverables**: Real-time event broadcasting pipeline connected from worker to frontend.
- **Acceptance Criteria**: Subscribed clients receive live status, progress, and logs without page reloads.
- **Testing Requirements**: SignalR connection, disconnection, reconnect sync tests.
- **Current Status**: Pending.

---

## Phase 5 — AI Failure Analysis & Troubleshooting
- **Phase ID**: `PHASE-05`
- **Objective**: Implement automated failure analysis with provider abstraction, privacy sanitization, and structured AI suggestions.
- **Dependencies**: `PHASE-04`.
- **Tasks**:
  - `P5-01`: Create `IAiAnalysisProvider` interface with structured response schema.
  - `P5-02`: Implement `MockAiProvider` with deterministic diagnostic heuristics.
  - `P5-03`: Implement `OpenAiProvider` (OpenAI / Azure OpenAI compatible).
  - `P5-04`: Implement `OllamaAiProvider` for local LLMs.
  - `P5-05`: Build `TelemetrySanitizer` to scrub credentials, tokens, and PII from error logs.
  - `P5-06`: Implement `AIAnalysisWorker` to automatically analyze failed executions.
  - `P5-07`: Implement AI analysis endpoints (`GET /api/executions/{id}/ai-analysis`, `POST /api/executions/{id}/analyze`).
- **Expected Deliverables**: Working AI failure analysis module returning structured troubleshooting insights.
- **Acceptance Criteria**: Failed executions automatically trigger sanitized AI analysis and emit SignalR notifications.
- **Testing Requirements**: Sanitizer unit tests, mock provider tests, fallback tests.
- **Current Status**: Pending.

---

## Phase 6 — Frontend SaaS Application
- **Phase ID**: `PHASE-06`
- **Objective**: Build a responsive React + TypeScript + Vite + Tailwind CSS frontend application.
- **Dependencies**: `PHASE-05`.
- **Tasks**:
  - `P6-01`: Initialize Vite + React + TypeScript + Tailwind CSS structure.
  - `P6-02`: Implement authentication pages (Login, Register) and AuthContext.
  - `P6-03`: Implement Main Layout (Navbar, Sidebar, Notification Drawer).
  - `P6-04`: Implement Overview Dashboard with KPI metric cards, status charts, and recent activity.
  - `P6-05`: Implement Job Management page (List, Search, Create/Edit Modal, Run Now action).
  - `P6-06`: Implement Live Execution Monitor (real-time progress bar, status badge, live streaming logs terminal).
  - `P6-07`: Implement Execution History page with filters, duration, attempt lineage, and retry triggers.
  - `P6-08`: Implement AI Failure Analysis modal/drawer with probable causes, solutions, and confidence badges.
  - `P6-09`: Implement Schedule Management page (Cron builder, next run time, enable/disable toggle).
  - `P6-10`: Implement System Settings & AI Provider status view.
- **Expected Deliverables**: Polished, responsive, functional web UI.
- **Acceptance Criteria**: Complete UI workflows verified against backend APIs and SignalR feeds.
- **Testing Requirements**: Component integration tests, SignalR UI tests, responsive layout checks.
- **Current Status**: Pending.

---

## Phase 7 — Automated Testing, Scenarios & Security
- **Phase ID**: `PHASE-07`
- **Objective**: Validate the platform through automated unit/integration tests and end-to-end verification scenarios A through F.
- **Dependencies**: `PHASE-06`.
- **Tasks**:
  - `P7-01`: Write xUnit unit tests for StateMachine, Handlers, and Sanitizer.
  - `P7-02`: Write integration tests for Job CRUD and Execution pipeline.
  - `P7-03`: Execute Scenario A: Successful Job run with live progress and completion.
  - `P7-04`: Execute Scenario B: Simulated Failure with AI analysis and retry lineage.
  - `P7-05`: Execute Scenario C: Scheduled recurring execution.
  - `P7-06`: Execute Scenario D: Cooperative job cancellation.
  - `P7-07`: Execute Scenario E: Application restart and queue survival / recovery.
  - `P7-08`: Execute Scenario F: SignalR disconnect and reconnect synchronization.
  - `P7-09`: Conduct security and authorization audit (RBAC, sanitization, SQL safety).
- **Expected Deliverables**: Test suite, verification logs, and updated `TESTING.md`.
- **Acceptance Criteria**: All scenarios pass; zero unresolved critical issues.
- **Testing Requirements**: Full regression run and scenario documentation.
- **Current Status**: Pending.

---

## Phase 8 — Containerization, Deployment & Final Release
- **Phase ID**: `PHASE-08`
- **Objective**: Create Docker Compose environment, documentation, and final deployment artifacts.
- **Dependencies**: `PHASE-07`.
- **Tasks**:
  - `P8-01`: Create Dockerfile for ASP.NET Core backend.
  - `P8-02`: Create Dockerfile and Nginx configuration for React frontend.
  - `P8-03`: Create `docker-compose.yml` orchestrating PostgreSQL, Backend, Frontend, and optional Ollama.
  - `P8-04`: Update and finalize all documentation in `docs/`.
  - `P8-05`: Complete final verification report and handoff summary.
- **Expected Deliverables**: Containerized application with single-command `docker compose up` launch.
- **Acceptance Criteria**: Clean environment spin-up with health checks and zero configuration friction.
- **Testing Requirements**: Container health check validation.
- **Current Status**: Completed.

---

## Phase 9 — Full Dockerization & DevOps Automation
- **Phase ID**: `PHASE-09`
- **Objective**: Harden multi-stage container builds, developer overrides, orchestration networking, health checks, environment templates, and Makefile automation.
- **Dependencies**: `PHASE-08`.
- **Tasks**:
  - `DOCKER-001`: Multi-stage Backend Dockerfile with non-root security (`USER $APP_UID`) and layer-cached restore.
  - `DOCKER-002`: Multi-stage Frontend Dockerfile with deterministic `npm ci`, Nginx SPA routing, and WebSocket reverse proxy.
  - `DOCKER-003`: Full-stack Docker Compose and developer override configuration (`docker-compose.yml`, `docker-compose.override.yml`).
  - `DOCKER-004`: Makefile for unified Linux, macOS, and documented PowerShell container lifecycle management.
  - `DOCKER-005`: Environment configuration template (`.env.example`) and build cache exclusions (`.dockerignore`).
  - `DOCKER-006`: Container health checks (`pg_isready`, OpenAPI curl probe, Nginx `/healthz`) and bridge network isolation (`queuesense-network`).
  - `DOCKER-007`: Docker integration and runtime execution testing.
- **Expected Deliverables**: Hardened Docker ecosystem, `Makefile`, `.dockerignore` files, and `docs/DOCKER_SETUP.md`.
- **Acceptance Criteria**: `docker compose config` passes cleanly; zero secrets in image layers; reproducible builds.
- **Testing Requirements**: Configuration validation and container status checks.
- **Current Status**: Tasks DOCKER-001 through DOCKER-006 Completed; DOCKER-007 Blocked awaiting host firmware virtualization.
