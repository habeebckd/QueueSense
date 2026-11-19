import {
  AuthResponse,
  User,
  JobDefinition,
  JobExecution,
  JobLog,
  JobExecutionEvent,
  JobSchedule,
  AiAnalysisResult,
  NotificationItem,
  DashboardSummary,
  MetricPoint,
  PagedResult,
  JobStatus
} from '../types';

const API_BASE = '/api';

async function request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('jobflow_token');
  const headers = new Headers(options.headers || {});
  headers.set('Content-Type', 'application/json');

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 204) {
    return {} as T;
  }

  const data = await response.json();

  if (!response.ok) {
    const errorMsg = data?.error?.message || data?.message || 'An error occurred';
    throw new Error(errorMsg);
  }

  return (data.data !== undefined ? data.data : data) as T;
}

export const api = {
  auth: {
    login: (credentials: { email: string; password: string }) =>
      request<AuthResponse>('/auth/login', {
        method: 'POST',
        body: JSON.stringify(credentials),
      }),
    register: (userData: { name: string; email: string; password: string }) =>
      request<AuthResponse>('/auth/register', {
        method: 'POST',
        body: JSON.stringify(userData),
      }),
    me: () => request<User>('/auth/me'),
  },

  jobs: {
    list: (params: { page?: number; pageSize?: number; search?: string; isEnabled?: boolean } = {}) => {
      const query = new URLSearchParams();
      if (params.page) query.set('page', params.page.toString());
      if (params.pageSize) query.set('pageSize', params.pageSize.toString());
      if (params.search) query.set('search', params.search);
      if (params.isEnabled !== undefined) query.set('isEnabled', params.isEnabled.toString());
      return request<PagedResult<JobDefinition>>(`/jobs?${query.toString()}`);
    },
    get: (id: string) => request<JobDefinition>(`/jobs/${id}`),
    create: (data: Partial<JobDefinition>) =>
      request<JobDefinition>('/jobs', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    update: (id: string, data: Partial<JobDefinition>) =>
      request<JobDefinition>(`/jobs/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
      }),
    delete: (id: string) =>
      request<void>(`/jobs/${id}`, {
        method: 'DELETE',
      }),
    run: (id: string, configurationOverrides?: string) =>
      request<{ id: string; status: JobStatus }>(`/jobs/${id}/run`, {
        method: 'POST',
        body: JSON.stringify({ configurationOverrides }),
      }),
  },

  executions: {
    get: (id: string) => request<JobExecution>(`/executions/${id}`),
    listByJob: (jobId: string, params: { page?: number; pageSize?: number; status?: JobStatus } = {}) => {
      const query = new URLSearchParams();
      if (params.page) query.set('page', params.page.toString());
      if (params.pageSize) query.set('pageSize', params.pageSize.toString());
      if (params.status) query.set('status', params.status);
      return request<PagedResult<JobExecution>>(`/jobs/${jobId}/executions?${query.toString()}`);
    },
    getActive: () => request<JobExecution[]>('/executions/active'),
    getLogs: (id: string, minSequenceNumber = 0) =>
      request<JobLog[]>(`/executions/${id}/logs?minSequenceNumber=${minSequenceNumber}`),
    getEvents: (id: string) => request<JobExecutionEvent[]>(`/executions/${id}/events`),
    cancel: (id: string) =>
      request<{ message: string }>(`/executions/${id}/cancel`, {
        method: 'POST',
      }),
    retry: (id: string) =>
      request<JobExecution>(`/executions/${id}/retry`, {
        method: 'POST',
      }),
  },

  schedules: {
    list: (jobId?: string) => {
      const query = jobId ? `?jobId=${jobId}` : '';
      return request<JobSchedule[]>(`/schedules${query}`);
    },
    create: (data: Partial<JobSchedule>) =>
      request<JobSchedule>('/schedules', {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    update: (id: string, data: Partial<JobSchedule>) =>
      request<JobSchedule>(`/schedules/${id}`, {
        method: 'PUT',
        body: JSON.stringify(data),
      }),
    delete: (id: string) =>
      request<void>(`/schedules/${id}`, {
        method: 'DELETE',
      }),
  },

  dashboard: {
    getSummary: () => request<DashboardSummary>('/dashboard/summary'),
    getMetrics: (timeRange = '24h') => request<MetricPoint[]>(`/dashboard/metrics?timeRange=${timeRange}`),
  },

  ai: {
    getAnalysis: (executionId: string) => request<AiAnalysisResult>(`/executions/${executionId}/ai-analysis`),
    triggerAnalysis: (executionId: string) =>
      request<{ message: string }>(`/executions/${executionId}/analyze`, {
        method: 'POST',
      }),
  },

  notifications: {
    list: (unreadOnly = false, limit = 20) =>
      request<NotificationItem[]>(`/notifications?unreadOnly=${unreadOnly}&limit=${limit}`),
    markRead: (id: string) =>
      request<{ message: string }>(`/notifications/${id}/read`, {
        method: 'PUT',
      }),
    markAllRead: () =>
      request<{ message: string }>('/notifications/read-all', {
        method: 'PUT',
      }),
  },
};
