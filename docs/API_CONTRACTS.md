# QueueSense — REST API Contracts & Specifications

All API endpoints follow RESTful conventions, accept/produce `application/json`, use standard HTTP response codes, and return consistent response envelopes.

---

## 1. Standard Response Envelope & Error Format

### 1.1 Success Response Wrapper
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

### 1.2 Error Response Wrapper
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "One or more validation errors occurred.",
    "details": [
      { "field": "cronExpression", "message": "Invalid 5-field cron format" }
    ]
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

---

## 2. Authentication Endpoints

### POST `/api/auth/register`
- **Description**: Registers a new user account.
- **Request**:
```json
{
  "name": "Alex Miller",
  "email": "alex@jobflow.ai",
  "password": "SecurePassword123!"
}
```
- **Response** `201 Created`:
```json
{
  "success": true,
  "data": {
    "userId": "d7b275ec-...",
    "name": "Alex Miller",
    "email": "alex@jobflow.ai",
    "role": "Operator",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6..."
  }
}
```

### POST `/api/auth/login`
- **Description**: Authenticates user credentials and returns JWT bearer token.
- **Request**:
```json
{
  "email": "admin@jobflow.ai",
  "password": "AdminPassword123!"
}
```
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "userId": "9a1e8a...",
    "name": "System Admin",
    "email": "admin@jobflow.ai",
    "role": "Admin",
    "token": "eyJhbGciOiJIUzI1Ni..."
  }
}
```

### GET `/api/auth/me`
- **Headers**: `Authorization: Bearer <token>`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "userId": "9a1e8a...",
    "name": "System Admin",
    "email": "admin@jobflow.ai",
    "role": "Admin"
  }
}
```

---

## 3. Job Management Endpoints

### GET `/api/jobs`
- **Query Params**: `page=1`, `pageSize=10`, `search=sample`, `isEnabled=true`, `handlerType=SampleDataProcessing`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "e67451a9-b3a1-4389-8d75-c54641eeec4a",
        "name": "Customer Data Sync",
        "description": "ETL batch processing for customer records",
        "handlerType": "SampleDataProcessingJobHandler",
        "configuration": "{\"batchSize\": 100, \"source\": \"crm\"}",
        "isEnabled": true,
        "createdAt": "2026-11-11T05:00:00Z",
        "lastExecutionStatus": "Completed",
        "lastRunAt": "2026-11-11T10:30:00Z"
      }
    ],
    "totalCount": 25,
    "page": 1,
    "pageSize": 10
  }
}
```

### GET `/api/jobs/{id}`
- **Response** `200 OK`: Full job definition with associated schedules and recent 5 executions.

### POST `/api/jobs`
- **Request**:
```json
{
  "name": "Nightly Financial Reconciliation",
  "description": "Calculates ledger balance adjustments",
  "handlerType": "BatchRecordProcessingJobHandler",
  "configuration": "{\"recordCount\": 500, \"simulateFailureRate\": 0}",
  "isEnabled": true
}
```
- **Response** `201 Created`: Created `JobDefinition` object.

### PUT `/api/jobs/{id}`
- **Request**: Updated fields.
- **Response** `200 OK`: Updated `JobDefinition` object.

### DELETE `/api/jobs/{id}`
- **Response** `204 No Content`. Soft-deletes or disables the job and cancels active schedules.

---

## 4. Execution Endpoints

### POST `/api/jobs/{id}/run`
- **Description**: Triggers immediate execution ("Run Now").
- **Request**: Optional parameter overrides `{"recordCount": 200}`.
- **Response** `202 Accepted`:
```json
{
  "success": true,
  "data": {
    "executionId": "8b9487c6-2c5e-4509-90d5-538adbe55fe1",
    "jobId": "e67451a9-b3a1-4389-8d75-c54641eeec4a",
    "status": "Queued",
    "queuedAt": "2026-11-11T11:50:00Z",
    "attemptNumber": 1
  }
}
```

### GET `/api/jobs/{id}/executions`
- **Query Params**: `page=1`, `pageSize=10`, `status=Failed`
- **Response** `200 OK`: Paginated executions for the given job.

### GET `/api/executions/{id}`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "id": "8b9487c6-2c5e-4509-90d5-538adbe55fe1",
    "jobDefinitionId": "e67451a9-b3a1-4389-8d75-c54641eeec4a",
    "jobName": "Customer Data Sync",
    "status": "Running",
    "progress": 65,
    "attemptNumber": 1,
    "parentExecutionId": null,
    "queuedAt": "2026-11-11T11:50:00Z",
    "startedAt": "2026-11-11T11:50:02Z",
    "completedAt": null,
    "workerId": "worker-alpha-01",
    "durationSeconds": 14.5,
    "errorMessage": null,
    "result": null
  }
}
```

### GET `/api/executions/{id}/logs`
- **Query Params**: `minSequenceNumber=0`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": [
    {
      "id": "1",
      "level": "Information",
      "message": "Job execution started by worker-alpha-01",
      "timestamp": "2026-11-11T11:50:02.100Z",
      "sequenceNumber": 1
    },
    {
      "id": "2",
      "level": "Information",
      "message": "Processed batch chunk 1/5 [100 records]",
      "timestamp": "2026-11-11T11:50:04.320Z",
      "sequenceNumber": 2
    }
  ]
}
```

### GET `/api/executions/{id}/events`
- **Response** `200 OK`: Complete state transition audit events.

### POST `/api/executions/{id}/cancel`
- **Response** `200 OK`: Confirms cancellation request sent.

### POST `/api/executions/{id}/retry`
- **Response** `202 Accepted`: Creates new execution attempt linked via `parentExecutionId`.

---

## 5. Scheduling Endpoints

### GET `/api/schedules`
- **Query Params**: `jobId=...`, `isEnabled=true`
- **Response** `200 OK`: List of all configured schedules.

### POST `/api/schedules`
- **Request**:
```json
{
  "jobDefinitionId": "e67451a9-b3a1-4389-8d75-c54641eeec4a",
  "scheduleType": "RecurringCron",
  "cronExpression": "0 */2 * * *",
  "timeZone": "UTC",
  "isEnabled": true
}
```
- **Response** `201 Created`: Schedule entity with computed `nextRunAt`.

### PUT `/api/schedules/{id}`
- **Request**: Updated schedule details.
- **Response** `200 OK`.

### DELETE `/api/schedules/{id}`
- **Response** `204 No Content`.

---

## 6. Dashboard & Monitoring Endpoints

### GET `/api/dashboard/summary`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "totalJobs": 18,
    "activeJobs": 14,
    "runningExecutions": 2,
    "completedExecutions": 340,
    "failedExecutions": 12,
    "successRatePercentage": 96.59,
    "averageDurationSeconds": 8.42
  }
}
```

### GET `/api/dashboard/metrics`
- **Query Params**: `timeRange=24h`
- **Response** `200 OK`: Hourly/daily aggregate time-series for chart rendering (runs, successes, failures).

### GET `/api/executions/active`
- **Response** `200 OK`: List of all currently `Queued` and `Running` executions.

---

## 7. AI Analysis Endpoints

### GET `/api/executions/{id}/ai-analysis`
- **Response** `200 OK`:
```json
{
  "success": true,
  "data": {
    "id": "771b9c90-...",
    "executionId": "8b9487c6-...",
    "summary": "Simulated database connection timeout during transactional commit.",
    "probableCauses": [
      "Target database connection pool exhaustion",
      "Network latency spike exceeding socket timeout threshold"
    ],
    "suggestedSolutions": [
      "Verify database connection pool max size",
      "Check database active lock contention",
      "Increase command timeout from 15s to 30s"
    ],
    "severity": "High",
    "confidence": "High",
    "retryRecommended": true,
    "createdAt": "2026-11-11T11:51:00Z"
  }
}
```

### POST `/api/executions/{id}/analyze`
- **Description**: Manually re-triggers AI analysis on a failed execution.
- **Response** `202 Accepted`.

---

## 8. Notifications Endpoints

### GET `/api/notifications`
- **Query Params**: `unreadOnly=false`, `limit=20`
- **Response** `200 OK`: List of notification records.

### PUT `/api/notifications/{id}/read`
- **Response** `200 OK`: Marks notification as read.
