# QueueSense — System Architecture Document

## 1. High-Level Architecture Overview

QueueSense uses a clean, decoupled modular monolithic architecture designed for extreme reliability, clear separation of concerns, and future horizontal microservice extraction.

```mermaid
flowchart TB
    subgraph ClientLayer ["Client Layer (Browser)"]
        UI["React 18 + Vite + Tailwind UI"]
        SignalRClient["@microsoft/signalr Client"]
        TanStack["TanStack Query (Cache & State)"]
    end

    subgraph ApiGateway ["API & Presentation Layer (JobFlow.Api)"]
        Controllers["REST API Controllers"]
        Hub["JobFlowHub (SignalR Hub)"]
        AuthMiddleware["JWT Authentication & RBAC"]
        Swagger["OpenAPI / Swagger"]
    end

    subgraph CoreLayer ["Domain & Core Layer (JobFlow.Core)"]
        Entities["Domain Entities & Enums"]
        Interfaces["Repository & Service Interfaces"]
        DTOs["Request / Response DTOs"]
        StateMachine["Job State Machine Validator"]
    end

    subgraph ServiceLayer ["Infrastructure Layer (JobFlow.Infrastructure)"]
        EFCore["EF Core DbContext (Npgsql)"]
        AiServices["AI Provider Engine (OpenAI / Ollama / Mock)"]
        Sanitizer["Prompt Data Sanitizer"]
        NotificationMgr["Notification & Dispatcher Service"]
    end

    subgraph WorkerLayer ["Background Processing Layer (JobFlow.Worker)"]
        ExecWorker["JobExecutionWorker"]
        SchedWorker["JobSchedulerWorker"]
        MonitorWorker["JobMonitoringWorker"]
        AiWorker["AIAnalysisWorker"]
        HandlerRegistry["JobHandlerRegistry (Allowlisted Handlers)"]
    end

    subgraph StorageLayer ["Persistence Layer"]
        Postgres[(PostgreSQL Database)]
    end

    ClientLayer <-->|REST API / JSON| ApiGateway
    ClientLayer <-->|WebSocket / SSE| Hub
    ApiGateway --> CoreLayer
    ApiGateway --> ServiceLayer
    WorkerLayer --> CoreLayer
    WorkerLayer --> ServiceLayer
    ServiceLayer --> Postgres
```

---

## 2. Component Responsibilities

### 2.1 Frontend (`JobFlow.Client` / `src/frontend`)
- **Technology**: React 18, TypeScript, Vite, Tailwind CSS, Lucide Icons, Recharts, TanStack Query, React Router v6.
- **Responsibilities**:
  - Provides intuitive SaaS UI with Overview, Job Management, Live Monitor, History, AI Analysis, Schedule Management, and Settings.
  - Maintains resilient SignalR connection with automatic reconnect and fallback to polling.
  - Renders real-time progress bars, streamed execution logs, and interactive failure diagnostics.

### 2.2 Backend Web API (`JobFlow.Api`)
- **Technology**: ASP.NET Core 9 (or .NET 8), C#, Serilog, JWT Bearer Auth.
- **Responsibilities**:
  - Exposes RESTful endpoints for CRUD on jobs, schedules, executions, and metrics.
  - Hosts `JobFlowHub` SignalR endpoint with JWT query string authentication.
  - Enforces API input validation, global exception formatting, and CORS policies.

### 2.3 Domain Core (`JobFlow.Core`)
- **Responsibilities**:
  - Encapsulates domain entities (`JobDefinition`, `JobExecution`, `JobSchedule`, `JobLog`, `JobExecutionEvent`, `AIAnalysisResult`, `Notification`, `User`).
  - Defines execution state machine rules and transition guards.
  - Houses interfaces for Job Handlers, AI Providers, and Repositories.

### 2.4 Infrastructure & Data (`JobFlow.Infrastructure`)
- **Technology**: EF Core 9, Npgsql (PostgreSQL Provider), HttpClient.
- **Responsibilities**:
  - Configures EF Core database context with relations, indexes, and concurrency tokens.
  - Implements AI providers (`MockAiProvider`, `OpenAiProvider`, `OllamaAiProvider`).
  - Implements log sanitization to scrub passwords, tokens, and connection strings prior to AI transmission.
  - Manages database seeding (default admin user, sample jobs, demo schedules).

### 2.5 Background Engine (`JobFlow.Worker`)
- **Technology**: .NET `BackgroundService` / `IHostedService`, periodic timers, channel/polling queues.
- **Four Dedicated Background Services**:
  1. `JobExecutionWorker`: Polls the durable queue, acquires distributed database leases, instantiates allowlisted handlers, executes with `CancellationToken`, streams progress and logs, and captures outcomes.
  2. `JobSchedulerWorker`: Computes recurring cron schedules and triggers due executions.
  3. `JobMonitoringWorker`: Scans for abandoned/orphaned executions (expired worker lease) and recovers them, plus flags timeouts.
  4. `AIAnalysisWorker`: Listens for failed executions, prepares sanitized error telemetry, invokes AI provider, persists structured analysis, and notifies users via SignalR.

---

## 3. Durable Database Queue Design

Unlike in-memory queues (e.g., `System.Threading.Channels`), QueueSense stores all queue state in PostgreSQL.

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant API as JobFlow.Api
    participant DB as PostgreSQL
    participant Worker as JobExecutionWorker
    participant Handler as Allowlisted Handler
    participant Hub as SignalR Hub
    participant AIWorker as AIAnalysisWorker

    User->>API: POST /api/jobs/{id}/run
    API->>DB: INSERT JobExecution (Status=Queued, QueuedAt=UTC)
    API->>Hub: Broadcast JobQueued
    API-->>User: 202 Accepted (ExecutionId)

    loop Worker Polling Loop (e.g. Every 1-2s)
        Worker->>DB: BEGIN TX: SELECT * FROM JobExecutions WHERE Status='Queued' LIMIT 1 FOR UPDATE SKIP LOCKED
        Worker->>DB: UPDATE JobExecutions SET Status='Running', WorkerId='worker-1', LeaseExpiresAt=NOW()+5m
        Worker->>DB: COMMIT TX
    end

    Worker->>Hub: Broadcast JobStarted
    Worker->>Handler: ExecuteAsync(context, cancellationToken)

    loop During Execution
        Handler->>Worker: ReportProgress(45%, "Processing chunk 3/10")
        Worker->>DB: INSERT JobLog, UPDATE Progress
        Worker->>Hub: Broadcast JobProgressUpdated & JobLogAdded
    end

    alt Execution Success
        Handler-->>Worker: Completed Result
        Worker->>DB: UPDATE JobExecutions SET Status='Completed', CompletedAt=UTC
        Worker->>Hub: Broadcast JobCompleted
    else Execution Failure / Exception
        Handler-->>Worker: Exception Thrown
        Worker->>DB: UPDATE JobExecutions SET Status='Failed', ErrorMessage=..., CompletedAt=UTC
        Worker->>Hub: Broadcast JobFailed
        Worker->>DB: INSERT AIAnalysisQueue / Notification
        AIWorker->>DB: Read Failed Execution & Logs
        AIWorker->>AIWorker: Sanitize Payload & Call AI Provider
        AIWorker->>DB: INSERT AIAnalysisResult
        AIWorker->>Hub: Broadcast AIAnalysisCompleted
    end
```

---

## 4. Concurrency, Locking & Lease Management
- **Safe Claiming**: Utilizes PostgreSQL row-level locking (`FOR UPDATE SKIP LOCKED`) or conditional atomic update (`UPDATE ... WHERE Status = 'Queued' AND WorkerId IS NULL`).
- **Heartbeat & Lease Expiration**: While running, the worker refreshes `LeaseExpiresAt` periodically.
- **Crash Recovery**: If a worker process abruptly dies, `JobMonitoringWorker` detects executions in `Running` status whose `LeaseExpiresAt` is in the past, marks them as stalled/recovered, and either retries them or records a `TimedOut`/`WorkerCrashed` failure event.

---

## 5. Security & Isolation Architecture
1. **Allowlisted Handlers Only**: Job handler keys match registered classes in `JobHandlerRegistry`. No `System.Diagnostics.Process`, no dynamic compilation, no `eval`.
2. **AI Privacy Boundary**: Telemetry scrubbing engine strips RFC 3986 credentials, JWTs (`ey...`), API keys (`sk-...`), and database connection strings before external LLM transmission.
3. **RBAC & Endpoint Protection**: All endpoints require JWT authentication except `/api/auth/login` and `/api/auth/register`.
