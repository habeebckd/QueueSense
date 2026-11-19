import * as signalR from '@microsoft/signalr';
import {
  JobEventPayload,
  JobProgressPayload,
  JobLogPayload,
  AiAnalysisEventPayload,
  NotificationPayload,
  ConnectionStatus
} from '../types';

type Listener<T> = (data: T) => void;

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private statusListeners: Set<(status: ConnectionStatus) => void> = new Set();
  private jobQueuedListeners: Set<Listener<JobEventPayload>> = new Set();
  private jobStartedListeners: Set<Listener<JobEventPayload>> = new Set();
  private jobProgressListeners: Set<Listener<JobProgressPayload>> = new Set();
  private jobLogListeners: Set<Listener<JobLogPayload>> = new Set();
  private jobCompletedListeners: Set<Listener<JobEventPayload>> = new Set();
  private jobFailedListeners: Set<Listener<JobEventPayload>> = new Set();
  private jobCancelledListeners: Set<Listener<JobEventPayload>> = new Set();
  private jobTimedOutListeners: Set<Listener<JobEventPayload>> = new Set();
  private aiAnalysisStartedListeners: Set<Listener<JobEventPayload>> = new Set();
  private aiAnalysisCompletedListeners: Set<Listener<AiAnalysisEventPayload>> = new Set();
  private notificationListeners: Set<Listener<NotificationPayload>> = new Set();

  public status: ConnectionStatus = 'disconnected';

  public async start(): Promise<void> {
    const token = localStorage.getItem('jobflow_token');
    if (!token) return;

    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/jobflow', {
        accessTokenFactory: () => localStorage.getItem('jobflow_token') || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.registerHandlers();

    try {
      this.updateStatus('reconnecting');
      await this.connection.start();
      this.updateStatus('connected');
    } catch {
      this.updateStatus('disconnected');
    }
  }

  public async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.updateStatus('disconnected');
    }
  }

  public async subscribeToExecution(executionId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('SubscribeToExecution', executionId);
      } catch (err) {
        console.error('Failed to subscribe to execution group:', err);
      }
    }
  }

  public async unsubscribeFromExecution(executionId: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('UnsubscribeFromExecution', executionId);
      } catch (err) {
        console.error('Failed to unsubscribe from execution group:', err);
      }
    }
  }

  private updateStatus(newStatus: ConnectionStatus) {
    this.status = newStatus;
    this.statusListeners.forEach((listener) => listener(newStatus));
  }

  private registerHandlers() {
    if (!this.connection) return;

    this.connection.onreconnecting(() => this.updateStatus('reconnecting'));
    this.connection.onreconnected(() => this.updateStatus('connected'));
    this.connection.onclose(() => this.updateStatus('disconnected'));

    this.connection.on('ReceiveJobQueued', (p: JobEventPayload) => this.jobQueuedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobStarted', (p: JobEventPayload) => this.jobStartedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobProgressUpdated', (p: JobProgressPayload) => this.jobProgressListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobLogAdded', (p: JobLogPayload) => this.jobLogListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobCompleted', (p: JobEventPayload) => this.jobCompletedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobFailed', (p: JobEventPayload) => this.jobFailedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobCancelled', (p: JobEventPayload) => this.jobCancelledListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveJobTimedOut', (p: JobEventPayload) => this.jobTimedOutListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveAIAnalysisStarted', (p: JobEventPayload) => this.aiAnalysisStartedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveAIAnalysisCompleted', (p: AiAnalysisEventPayload) => this.aiAnalysisCompletedListeners.forEach((l) => l(p)));
    this.connection.on('ReceiveNotification', (p: NotificationPayload) => this.notificationListeners.forEach((l) => l(p)));
  }

  public onStatusChange(listener: (status: ConnectionStatus) => void): () => void {
    this.statusListeners.add(listener);
    listener(this.status);
    return () => this.statusListeners.delete(listener);
  }

  public onJobProgress(listener: Listener<JobProgressPayload>): () => void {
    this.jobProgressListeners.add(listener);
    return () => this.jobProgressListeners.delete(listener);
  }

  public onJobLog(listener: Listener<JobLogPayload>): () => void {
    this.jobLogListeners.add(listener);
    return () => this.jobLogListeners.delete(listener);
  }

  public onJobCompleted(listener: Listener<JobEventPayload>): () => void {
    this.jobCompletedListeners.add(listener);
    return () => this.jobCompletedListeners.delete(listener);
  }

  public onJobFailed(listener: Listener<JobEventPayload>): () => void {
    this.jobFailedListeners.add(listener);
    return () => this.jobFailedListeners.delete(listener);
  }

  public onJobCancelled(listener: Listener<JobEventPayload>): () => void {
    this.jobCancelledListeners.add(listener);
    return () => this.jobCancelledListeners.delete(listener);
  }

  public onAiAnalysisCompleted(listener: Listener<AiAnalysisEventPayload>): () => void {
    this.aiAnalysisCompletedListeners.add(listener);
    return () => this.aiAnalysisCompletedListeners.delete(listener);
  }

  public onNotification(listener: Listener<NotificationPayload>): () => void {
    this.notificationListeners.add(listener);
    return () => this.notificationListeners.delete(listener);
  }
}

export const signalRService = new SignalRService();
