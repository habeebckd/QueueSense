# QueueSense — Background Job Lifecycle & Workflow Specifications

This document defines the complete state machine, execution lifecycle, transitions, fault tolerance, and recovery protocols in QueueSense.

---

## 1. Execution State Machine

```mermaid
stateDiagram-v2
    [*] --> Pending: Created (Scheduled for later)
    [*] --> Queued: Run Now triggered / Scheduled time reached
    
    Pending --> Queued: Trigger due timestamp
    Pending --> Cancelled: Cancelled before queueing
    
    Queued --> Running: Worker claims lease
    Queued --> Cancelled: Cancel requested while queued
    
    Running --> Completed: Handler completed successfully
    Running --> Failed: Unhandled exception or handler failure
    Running --> TimedOut: Execution exceeded MaxExecutionDuration
    Running --> CancellationRequested: User requests cancellation
    
    CancellationRequested --> Cancelled: Handler observed cancellation token
    CancellationRequested --> Failed: Handler threw unhandled abort error
    
    Failed --> Queued: User triggers Retry / Auto-retry policy
    TimedOut --> Queued: User triggers Retry
    
    Completed --> [*]
    Cancelled --> [*]
```

### 1.1 Valid State Transitions Table

| Previous Status | New Status | Trigger / Condition | Valid Next Action |
|---|---|---|---|
| `Pending` | `Queued` | Scheduled time reached (`NextRunAt <= UTC`) | Worker will claim |
| `Pending` | `Cancelled` | User cancels scheduled run | Terminal state |
| `Queued` | `Running` | Worker claims execution lease via atomic DB update | Execution starts |
| `Queued` | `Cancelled` | User cancels before worker claims | Terminal state |
| `Running` | `Completed` | Handler completes execution cleanly | Terminal state |
| `Running` | `Failed` | Handler throws exception or fails explicitly | AI Analysis triggered |
| `Running` | `TimedOut` | Execution duration exceeds configured limit | AI Analysis / Retry |
| `Running` | `CancellationRequested` | User triggers POST `/api/executions/{id}/cancel` | Token signalled |
| `CancellationRequested` | `Cancelled` | Handler confirms cancellation | Terminal state |
| `CancellationRequested` | `Failed` | Handler aborted abruptly with error | AI Analysis |
| `Failed` | `Queued` (New Attempt) | User clicks Retry or policy retries | New linked execution record |
| `TimedOut` | `Queued` (New Attempt) | User clicks Retry | New linked execution record |

---

## 2. End-to-End Lifecycle Sequence Diagram

```mermaid
sequenceDiagram
    autonumber
    actor User
    participant Frontend as React Client
    participant Hub as SignalR Hub
    participant API as ASP.NET Core API
    participant DB as PostgreSQL
    participant Worker as JobExecutionWorker
    participant Handler as Allowlisted Handler
    participant AIWorker as AIAnalysisWorker
    participant AI as AI Engine (Mock / OpenAI / Ollama)

    Note over User, Frontend: 1. Creation & Trigger
    User->>Frontend: Click "Run Now"
    Frontend->>API: POST /api/jobs/{id}/run
    API->>DB: INSERT JobExecution (Status=Queued, AttemptNumber=1)
    API->>DB: INSERT JobExecutionEvent (Type=Queued)
    API->>Hub: Broadcast "JobQueued"
    Hub-->>Frontend: Live update: Status=Queued
    API-->>Frontend: 202 Accepted { executionId: "..." }

    Note over Worker, Handler: 2. Worker Claim & Start
    loop Queue Polling (1-2s interval)
        Worker->>DB: SELECT & UPDATE JobExecution WHERE Status='Queued' LIMIT 1
    end
    Worker->>DB: UPDATE JobExecution SET Status='Running', WorkerId='worker-1', LeaseExpiresAt=UTC+5m
    Worker->>DB: INSERT JobExecutionEvent (Type=Running)
    Worker->>Hub: Broadcast "JobStarted"
    Hub-->>Frontend: Live update: Status=Running, Progress=0%

    Note over Handler, Frontend: 3. Execution, Progress & Streaming Logs
    Worker->>Handler: ExecuteAsync(context, cancellationToken)
    loop Iterative Steps
        Handler->>Worker: ReportProgress(progressPercentage, logMessage)
        Worker->>DB: UPDATE JobExecution SET Progress=..., INSERT JobLog
        Worker->>Hub: Broadcast "JobProgressUpdated", "JobLogAdded"
        Hub-->>Frontend: Update Progress Bar & Terminal Logs
    end

    Note over Handler, AIWorker: 4. Failure Branch & AI Diagnosis
    alt Normal Completion
        Handler-->>Worker: Success (Result JSON)
        Worker->>DB: UPDATE JobExecution SET Status='Completed', CompletedAt=UTC
        Worker->>DB: INSERT JobExecutionEvent (Type=Completed)
        Worker->>Hub: Broadcast "JobCompleted"
        Hub-->>Frontend: Show Completed Badge
    else Failure / Exception Occurred
        Handler-->>Worker: Throws Exception (e.g. DbTimeoutException)
        Worker->>DB: UPDATE JobExecution SET Status='Failed', ErrorMessage=..., CompletedAt=UTC
        Worker->>DB: INSERT JobExecutionEvent (Type=Failed)
        Worker->>Hub: Broadcast "JobFailed"
        Hub-->>Frontend: Show Failed Badge
        
        Note over AIWorker, AI: 5. Asynchronous AI Failure Analysis
        Worker->>DB: INSERT AIAnalysisQueue (ExecutionId)
        AIWorker->>DB: Fetch Failed Execution, Job Definition, and Recent Logs
        AIWorker->>AIWorker: TelemetrySanitizer: Scrub Passwords, Tokens & Sensitive Keys
        AIWorker->>Hub: Broadcast "AIAnalysisStarted"
        AIWorker->>AI: Send Structured Diagnostic Prompt
        AI-->>AIWorker: Return JSON: { summary, probableCauses, suggestedSolutions, severity, confidence, retryRecommended }
        AIWorker->>DB: INSERT AIAnalysisResult
        AIWorker->>DB: INSERT Notification (Type=AiAnalysisReady)
        AIWorker->>Hub: Broadcast "AIAnalysisCompleted", "NotificationReceived"
        Hub-->>Frontend: Display AI Failure Diagnosis Card with Recommended Actions
    end
```

---

## 3. Worker Crash & Stalled Job Recovery Workflow

```mermaid
flowchart TD
    StartCheck["JobMonitoringWorker: Periodic Heartbeat Check (Every 30s)"] --> QueryStalled["Query DB: Status='Running' AND LeaseExpiresAt < UTC_NOW"]
    QueryStalled --> HasStalled{Any Stalled Jobs?}
    HasStalled -- No --> Wait["Sleep until next interval"]
    HasStalled -- Yes --> Iterate["Loop each stalled execution"]

    Iterate --> CheckAttempt{"Attempt < MaxRetries?"}
    CheckAttempt -- Yes --> RecoverRetry["Mark Status='Queued' or Create Retry Attempt<br/>Log Event: 'RecoveredFromWorkerCrash'<br/>Clear WorkerId & Extend Lease"]
    RecoverRetry --> NotifyRecovery["Broadcast SignalR: JobQueued (Recovery)"]
    CheckAttempt -- No --> MarkTimedOut["Mark Status='TimedOut' / 'Failed'<br/>Reason: 'Worker lease expired without renewal (Worker Crash Detected)'"]
    MarkTimedOut --> TriggerAI["Queue for AI Failure Analysis"]
    TriggerAI --> NotifyFailure["Broadcast SignalR: JobTimedOut"]
    NotifyRecovery --> Wait
    NotifyFailure --> Wait
```

---

## 4. Cooperative Cancellation Protocol

1. **User Action**: User clicks "Cancel Job" on UI -> `POST /api/executions/{id}/cancel`.
2. **State Transition**: Backend transitions `Status` to `CancellationRequested`.
3. **Signal Delivery**:
   - The executing worker holds an internal dictionary `ConcurrentDictionary<Guid, CancellationTokenSource>`.
   - The cancellation request triggers `cts.Cancel()`.
4. **Handler Compliance**:
   - Handlers check `cancellationToken.ThrowIfCancellationRequested()` periodically during loops.
   - Handlers release resources and cleanly rethrow `OperationCanceledException`.
5. **State Finalization**:
   - The worker catches `OperationCanceledException`, records `Status = Cancelled`, writes cancellation audit log, and emits `JobCancelled` via SignalR.
