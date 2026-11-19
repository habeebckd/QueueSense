// JobFlow AI — TypeScript Type Definitions

export type JobStatus =
  | 'Pending'
  | 'Queued'
  | 'Running'
  | 'Completed'
  | 'Failed'
  | 'TimedOut'
  | 'CancellationRequested'
  | 'Cancelled';

export type UserRole = 'Admin' | 'Operator' | 'Viewer';

export type ScheduleType =
  | 'Immediate'
  | 'DelayedOnce'
  | 'RecurringCron'
  | 'RecurringInterval';

export type LogLevel = 'Debug' | 'Information' | 'Warning' | 'Error' | 'Critical';

export type Severity = 'Low' | 'Medium' | 'High' | 'Critical';

export type Confidence = 'Low' | 'Medium' | 'High';

export type ConnectionStatus = 'connected' | 'reconnecting' | 'disconnected';

export interface User {
  userId: string;
  name: string;
  email: string;
  role: UserRole;
  createdAt?: string;
}

export interface AuthResponse {
  userId: string;
  name: string;
  email: string;
  role: UserRole;
  token: string;
}

export interface JobDefinition {
  id: string;
  name: string;
  description?: string;
  handlerType: string;
  configuration?: string;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
  lastExecutionStatus?: JobStatus;
  lastRunAt?: string;
  schedules?: JobSchedule[];
}

export interface JobSchedule {
  id: string;
  jobDefinitionId: string;
  jobName?: string;
  scheduleType: ScheduleType;
  cronExpression?: string;
  timeZone: string;
  nextRunAt?: string;
  isEnabled: boolean;
}

export interface JobExecution {
  id: string;
  jobDefinitionId: string;
  jobName: string;
  handlerType: string;
  status: JobStatus;
  progress: number;
  attemptNumber: number;
  parentExecutionId?: string;
  queuedAt: string;
  startedAt?: string;
  completedAt?: string;
  workerId?: string;
  durationSeconds?: number;
  errorMessage?: string;
  result?: string;
}

export interface JobLog {
  id: string;
  executionId: string;
  level: LogLevel;
  message: string;
  timestamp: string;
  sequenceNumber: number;
}

export interface JobExecutionEvent {
  id: string;
  executionId: string;
  previousStatus: string;
  newStatus: string;
  reason?: string;
  timestamp: string;
  correlationId: string;
}

export interface AiAnalysisResult {
  id: string;
  executionId: string;
  summary: string;
  probableCauses: string[];
  suggestedSolutions: string[];
  severity: Severity;
  confidence: Confidence;
  retryRecommended: boolean;
  createdAt: string;
}

export interface NotificationItem {
  id: string;
  userId?: string;
  executionId?: string;
  type: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface DashboardSummary {
  totalJobs: number;
  activeJobs: number;
  runningExecutions: number;
  queuedExecutions: number;
  completedExecutions: number;
  failedExecutions: number;
  successRatePercentage: number;
  averageDurationSeconds: number;
}

export interface MetricPoint {
  timestamp: string;
  successCount: number;
  failureCount: number;
  averageDuration: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface JobEventPayload {
  eventId: string;
  jobId: string;
  executionId: string;
  eventType: string;
  timestamp: string;
  status: JobStatus;
  message?: string;
  metadata?: Record<string, unknown>;
}

export interface JobProgressPayload {
  eventId: string;
  jobId: string;
  executionId: string;
  eventType: string;
  timestamp: string;
  progressPercentage: number;
  status: JobStatus;
  message?: string;
}

export interface JobLogPayload {
  eventId: string;
  executionId: string;
  eventType: string;
  timestamp: string;
  sequenceNumber: number;
  level: string;
  message: string;
}

export interface AiAnalysisEventPayload {
  eventId: string;
  executionId: string;
  eventType: string;
  timestamp: string;
  summary: string;
  severity: Severity;
  confidence: Confidence;
  retryRecommended: boolean;
}

export interface NotificationPayload {
  id: string;
  userId?: string;
  executionId?: string;
  type: string;
  message: string;
  createdAt: string;
}
