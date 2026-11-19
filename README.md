# QueueSense — Intelligent Background Job Monitoring

> **AI-Powered Background Job Monitoring & Management Platform**  
> *Intelligent Background Job Monitoring for High-Reliability Engineering Teams*

QueueSense is an intelligent background job monitoring and management platform designed for developers and engineering teams. It provides real-time visibility into job execution, scheduling, progress tracking, execution logs, failures, and performance. Using AI-powered failure analysis and troubleshooting recommendations, QueueSense helps teams identify issues faster and maintain reliable background processing systems.

---

## Key Features

- **Durable PostgreSQL Queue**: Database-backed job queue guaranteeing job persistence across application restarts with atomic lease claiming.
- **Multi-Worker Background Architecture**:
  - `JobExecutionWorker`: Claims jobs atomically via leases, manages timeouts, and respects cooperative cancellations.
  - `JobSchedulerWorker`: Computes recurring Cron and interval-based triggers with duplicate execution prevention.
  - `JobMonitoringWorker`: Recovers orphaned jobs from crashed workers and flags stalled executions.
  - `AIAnalysisWorker`: Asynchronously diagnoses failed executions and recommends troubleshooting fixes.
- **Built-in Allowlisted Job Handlers**:
  1. *Sample Data Processing*: Simulates ETL data parsing, validation, and commit.
  2. *Batch Record Processing*: Simulates high-throughput batching with configurable failure rate.
  3. *Report Generation*: Simulates multi-section document export and analytics aggregation.
  4. *Simulated Failure Testing*: Triggers configurable exceptions to demonstrate AI diagnosis.
  5. *Configurable Long-Running*: Validates cooperative cancellation tokens and heartbeat leases.
- **AI Failure Diagnostics Engine**:
  - Pluggable AI provider abstraction (`Mock`, `OpenAI`, `Ollama`).
  - Automated telemetry sanitization (scrubs tokens, connection strings, credentials).
  - Structured output schema: root causes, remediation actions, severity, and retry recommendation.
- **Real-Time SignalR Dashboard**:
  - Live execution monitors with streaming logs and animated progress bars.
  - Reconnection resilience with state synchronization.
  - Interactive Recharts analytics for job duration, failure rates, and system throughput.
  - Primary developer-tool brand identity (`#00B1BA`).

---

## Architecture & Technology Stack

- **Backend**: ASP.NET Core 9 / C#, Entity Framework Core, PostgreSQL (Npgsql), SignalR, Serilog, BCrypt.
- **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, TanStack Query, Recharts, Lucide Icons.
- **Containerization**: Docker Compose with multi-stage builds.

---

## Quickstart

### Option 1: Docker Compose (Single Command)
```bash
docker compose up -d --build
```
- Dashboard: [http://localhost:3000](http://localhost:3000)
- Backend API / Swagger: [http://localhost:5000/swagger](http://localhost:5000/swagger)
- Default Login: `admin@jobflow.ai` / `AdminPassword123!`

### Option 2: Local Development
See [`docs/SETUP_GUIDE.md`](docs/SETUP_GUIDE.md) for full instructions.

---

## Documentation Index

- [Requirements Specification](docs/REQUIREMENTS.md)
- [System Architecture](docs/ARCHITECTURE.md)
- [Implementation Plan](docs/IMPLEMENTATION_PLAN.md)
- [Master Progress Tracker](docs/PROGRESS_TRACKER.md)
- [Job Lifecycle & State Machine](docs/JOB_WORKFLOW.md)
- [REST API Contracts](docs/API_CONTRACTS.md)
- [Database Schema & EF Core Design](docs/DATABASE_DESIGN.md)
- [AI Failure Analysis Architecture](docs/AI_INTEGRATION.md)
- [SignalR Real-Time Specifications](docs/SIGNALR_EVENTS.md)
- [Testing Matrix & Scenarios](docs/TESTING.md)
- [Issues & Fixes Log](docs/ISSUES_AND_FIXES.md)
- [Developer Setup Guide](docs/SETUP_GUIDE.md)
