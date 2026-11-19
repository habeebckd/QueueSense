# QueueSense — Requirements Specification

## 1. Executive Summary & Goals
**QueueSense** is an intelligent background job monitoring and management platform designed for developers and engineering teams. It provides real-time visibility into job execution, scheduling, progress tracking, execution logs, failures, and performance. Using AI-powered failure analysis and troubleshooting recommendations, QueueSense helps teams identify issues faster and maintain reliable background processing systems.

---

## 2. Stakeholders & User Personas
- **DevOps / Reliability Engineers**: Monitor background workloads, inspect failure rates, latency metrics, and system throughput.
- **Backend Developers**: Schedule batch operations, define job execution logic, inspect structured logs, review AI remediation insights.
- **Operations & Support**: Cancel stuck jobs, trigger manual retries with full lineage tracking, configure recurring job triggers.
- **Platform Administrators**: Manage user accounts, role-based access control (RBAC), and AI provider credentials.

---

## 3. Functional Requirements

### 3.1 Authentication & Authorization
- **FR-AUTH-01**: Secure user registration and login using JWT (JSON Web Tokens) with PBKDF2/BCrypt-based password hashing.
- **FR-AUTH-02**: Role-Based Access Control (Admin, Operator, Viewer).
- **FR-AUTH-03**: Token refresh mechanism and secure claims retrieval via `/api/auth/me`.
- **FR-AUTH-04**: SignalR connection authorization via query string token validation.

### 3.2 Job Definition Management
- **FR-JOB-01**: CRUD operations on job definitions (Name, Description, HandlerType, Configuration JSON, IsEnabled).
- **FR-JOB-02**: Handler allowlisting to prevent arbitrary command execution.
- **FR-JOB-03**: Custom parameter schemas validation per handler type.
- **FR-JOB-04**: Search, sorting, and pagination across job definitions.

### 3.3 Scheduling & Triggering
- **FR-SCHED-01**: Immediate on-demand execution ("Run Now").
- **FR-SCHED-02**: Delayed single-execution scheduling (`RunAt` timestamp).
- **FR-SCHED-03**: Recurring schedules supporting Standard Cron syntax and simple interval definitions.
- **FR-SCHED-04**: Timezone-aware schedule computation with durable tracking of `NextRunAt`.
- **FR-SCHED-05**: Concurrency controls to prevent duplicate overlapping runs for the same schedule.

### 3.4 Durable Queue & Execution Engine
- **FR-EXEC-01**: Database-backed durable queue that guarantees pending jobs survive process restarts.
- **FR-EXEC-02**: Atomic job claiming using worker leases (`WorkerId`, `LeaseExpiresAt`) with safe database locking.
- **FR-EXEC-03**: State machine enforcement:
  `Pending -> Queued -> Running -> Completed | Failed | TimedOut | Cancelled`.
- **FR-EXEC-04**: Graceful cancellation handling using `CancellationToken` cooperative termination.
- **FR-EXEC-05**: Worker crash detection and automatic abandonment recovery via lease expiration sweep.
- **FR-EXEC-06**: Bounded retries with explicit lineage tracking (`ParentExecutionId`, `AttemptNumber`).
- **FR-EXEC-07**: Structured log streaming with sequence numbers, log levels, and correlation IDs.

### 3.5 Real-Time Observability (SignalR)
- **FR-RT-01**: Typed SignalR hub broadcasting execution transitions (`JobQueued`, `JobStarted`, `JobProgressUpdated`, `JobLogAdded`, `JobCompleted`, `JobFailed`, `JobCancelled`, `JobTimedOut`).
- **FR-RT-02**: Reconnection resiliency: Automatic fallback to API polling upon disconnection and state sync upon reconnect.
- **FR-RT-03**: Live log streaming to active execution monitor page.

### 3.6 AI Failure Analysis & Troubleshooting
- **FR-AI-01**: Automatic asynchronous triggering of failure analysis upon job execution failure.
- **FR-AI-02**: Provider abstraction supporting:
  - Mock AI Provider (deterministic, zero external API key required).
  - OpenAI / Azure OpenAI (GPT-4o / GPT-3.5-Turbo).
  - Ollama (local open-weight LLMs like Llama 3, Mistral).
- **FR-AI-03**: Sensitive data sanitization: Stripping passwords, bearer tokens, API keys, and connection strings prior to LLM submission.
- **FR-AI-04**: Structured JSON output format with summary, probable causes, suggested remedies, severity, confidence, and retry recommendation.
- **FR-AI-05**: Non-blocking failure isolation: AI errors never fail or stall normal job execution cycles.

### 3.7 Built-in Job Handlers
- **SampleDataProcessingJobHandler**: Simulates ETL data parsing, normalization, and insertion with step-by-step progress.
- **BatchRecordProcessingJobHandler**: Simulates high-volume batch record processing with configurable batch size and failure simulation rate.
- **ReportGenerationJobHandler**: Simulates multi-page PDF/Excel statistical aggregation with file artifact result metadata.
- **SimulatedFailureJobHandler**: Deterministically throws configured exceptions (e.g., DatabaseTimeout, OutOfMemory, ExternalServiceUnavailable) to test AI failure diagnosis.
- **ConfigurableLongRunningJobHandler**: Simulates extended processing to test cancellation, heartbeat leases, and worker timeout mechanisms.

---

## 4. Non-Functional Requirements

### 4.1 Reliability & Resilience
- Background workers must resume queued work cleanly after unexpected process termination.
- Database transactions must wrap state transitions and event emission records.

### 4.2 Security
- No raw shell command execution or un-sandboxed code execution.
- Parameterized SQL via Entity Framework Core to eliminate SQL injection.
- Passwords hashed with cryptographic salts.
- Redaction of sensitive fields in log payloads and AI prompts.

### 4.3 Performance & Scalability
- Support for multiple concurrent worker threads via semaphore-based worker throttling.
- Indexed database queries for queue polling, status filtering, and execution history.
- Paginated APIs for logs, executions, and job lists.

### 4.4 Usability & UI/UX
- Responsive, modern SaaS dashboard with clear execution-status color indicators (Green=Completed, Red=Failed, Amber=Running/Queued, Gray=Cancelled).
- Recharts-powered analytics for throughput, failure rates, and execution durations.
- Toast notifications and modal dialogs for critical actions.
