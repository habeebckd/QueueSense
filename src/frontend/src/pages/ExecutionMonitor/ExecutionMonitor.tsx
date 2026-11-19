import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  Terminal,
  StopCircle,
  RotateCcw,
  Sparkles,
  Clock,
  Layers,
  Cpu,
  ArrowLeft
} from 'lucide-react';
import { api } from '../../services/api';
import { signalRService } from '../../services/signalr';
import { JobExecution, JobLog, AiAnalysisResult } from '../../types';
import { StatusBadge } from '../../components/common/StatusBadge';
import { ProgressBar } from '../../components/common/ProgressBar';
import { AiAnalysisModal } from '../AiAnalysis/AiAnalysisModal';

export const ExecutionMonitor: React.FC = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const executionIdParam = searchParams.get('executionId');

  const [activeExecutions, setActiveExecutions] = useState<JobExecution[]>([]);
  const [selectedExecutionId, setSelectedExecutionId] = useState<string | null>(executionIdParam);
  const [execution, setExecution] = useState<JobExecution | null>(null);
  const [logs, setLogs] = useState<JobLog[]>([]);
  const [aiAnalysis, setAiAnalysis] = useState<AiAnalysisResult | null>(null);
  const [aiModalOpen, setAiModalOpen] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [retrying, setRetrying] = useState(false);

  const logsTerminalRef = useRef<HTMLDivElement>(null);

  // 1. Fetch active executions or default execution
  useEffect(() => {
    async function loadActive() {
      try {
        const active = await api.executions.getActive();
        setActiveExecutions(active);
        if (!selectedExecutionId && active.length > 0) {
          setSelectedExecutionId(active[0].id);
        }
      } catch (err) {
        console.error('Error loading active executions:', err);
      }
    }
    loadActive();
  }, [selectedExecutionId]);

  // 2. Load execution details & initial logs
  useEffect(() => {
    if (!selectedExecutionId) return;

    let isMounted = true;

    async function loadDetails() {
      try {
        const [execData, logData] = await Promise.all([
          api.executions.get(selectedExecutionId!),
          api.executions.getLogs(selectedExecutionId!, 0),
        ]);
        if (isMounted) {
          setExecution(execData);
          setLogs(logData);

          // Check if AI analysis exists
          if (execData.status === 'Failed' || execData.status === 'TimedOut') {
            try {
              const analysis = await api.ai.getAnalysis(selectedExecutionId!);
              setAiAnalysis(analysis);
            } catch {
              setAiAnalysis(null);
            }
          }
        }
      } catch (err) {
        console.error('Error loading execution details:', err);
      }
    }

    loadDetails();

    // 3. Join SignalR execution group for real-time streaming logs
    signalRService.subscribeToExecution(selectedExecutionId);

    // 4. Attach real-time SignalR listeners
    const unsubProgress = signalRService.onJobProgress((p) => {
      if (p.executionId === selectedExecutionId) {
        setExecution((prev) => (prev ? { ...prev, progress: p.progressPercentage, status: p.status } : prev));
      }
    });

    const unsubLog = signalRService.onJobLog((l) => {
      if (l.executionId === selectedExecutionId) {
        setLogs((prev) => {
          if (prev.some((x) => x.sequenceNumber === l.sequenceNumber)) return prev;
          return [...prev, {
            id: l.eventId,
            executionId: l.executionId,
            level: l.level as any,
            message: l.message,
            timestamp: l.timestamp,
            sequenceNumber: l.sequenceNumber,
          }];
        });
      }
    });

    const unsubCompleted = signalRService.onJobCompleted((p) => {
      if (p.executionId === selectedExecutionId) {
        setExecution((prev) => (prev ? { ...prev, status: 'Completed', progress: 100 } : prev));
      }
    });

    const unsubFailed = signalRService.onJobFailed((p) => {
      if (p.executionId === selectedExecutionId) {
        setExecution((prev) => (prev ? { ...prev, status: 'Failed', errorMessage: p.message } : prev));
      }
    });

    const unsubCancelled = signalRService.onJobCancelled((p) => {
      if (p.executionId === selectedExecutionId) {
        setExecution((prev) => (prev ? { ...prev, status: 'Cancelled' } : prev));
      }
    });

    const unsubAi = signalRService.onAiAnalysisCompleted((p) => {
      if (p.executionId === selectedExecutionId) {
        api.ai.getAnalysis(selectedExecutionId).then(setAiAnalysis).catch(() => {});
      }
    });

    return () => {
      isMounted = false;
      signalRService.unsubscribeFromExecution(selectedExecutionId);
      unsubProgress();
      unsubLog();
      unsubCompleted();
      unsubFailed();
      unsubCancelled();
      unsubAi();
    };
  }, [selectedExecutionId]);

  // Auto-scroll terminal to bottom when new logs arrive
  useEffect(() => {
    if (logsTerminalRef.current) {
      logsTerminalRef.current.scrollTop = logsTerminalRef.current.scrollHeight;
    }
  }, [logs]);

  const handleCancel = async () => {
    if (!execution) return;
    if (confirm('Are you sure you want to cooperatively cancel this execution?')) {
      setCancelling(true);
      try {
        await api.executions.cancel(execution.id);
      } catch (err: unknown) {
        if (err instanceof Error) alert(err.message);
      } finally {
        setCancelling(false);
      }
    }
  };

  const handleRetry = async () => {
    if (!execution) return;
    setRetrying(true);
    try {
      const newAttempt = await api.executions.retry(execution.id);
      setSelectedExecutionId(newAttempt.id);
      navigate(`/monitor?executionId=${newAttempt.id}`);
    } catch (err: unknown) {
      if (err instanceof Error) alert(err.message);
    } finally {
      setRetrying(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/jobs')}
            className="p-2 border border-slate-200 hover:bg-slate-100 rounded-lg text-slate-500 transition-colors"
            title="Back to Jobs"
          >
            <ArrowLeft className="w-4 h-4" />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-slate-900 tracking-tight flex items-center gap-2">
              <span>Live Execution Monitor</span>
            </h1>
            <p className="text-sm text-slate-500">
              Real-time SignalR progress streaming, lease telemetry, and diagnostic logs.
            </p>
          </div>
        </div>

        {/* Execution Picker Dropdown if active */}
        {activeExecutions.length > 0 && (
          <div className="flex items-center gap-2">
            <span className="text-xs text-slate-500 font-medium">Active Runs:</span>
            <select
              value={selectedExecutionId || ''}
              onChange={(e) => {
                setSelectedExecutionId(e.target.value);
                navigate(`/monitor?executionId=${e.target.value}`);
              }}
              className="text-xs border border-slate-300 rounded-lg px-3 py-1.5 bg-white font-medium focus:ring-2 focus:ring-brand-500"
            >
              {activeExecutions.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.jobName} ({a.status} - {a.progress}%)
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      {!execution ? (
        <div className="bg-white rounded-xl border border-slate-200 p-12 text-center text-slate-400">
          <p className="text-base font-medium">No execution selected.</p>
          <p className="text-xs mt-1 text-slate-500">
            Select a running job from the dropdown or trigger "Run Now" from Job Management.
          </p>
        </div>
      ) : (
        <>
          {/* Status & Action Bar */}
          <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-6">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-6 border-b border-slate-100">
              <div>
                <div className="flex items-center gap-3 mb-1">
                  <h2 className="text-xl font-bold text-slate-900">{execution.jobName}</h2>
                  <StatusBadge status={execution.status} size="md" />
                  <span className="text-xs px-2 py-0.5 rounded bg-slate-100 text-slate-600 font-mono">
                    Attempt #{execution.attemptNumber}
                  </span>
                </div>
                <div className="flex flex-wrap items-center gap-4 text-xs text-slate-500 font-mono mt-2">
                  <span className="flex items-center gap-1">
                    <Layers className="w-3.5 h-3.5 text-slate-400" />
                    Handler: {execution.handlerType}
                  </span>
                  <span className="flex items-center gap-1">
                    <Cpu className="w-3.5 h-3.5 text-slate-400" />
                    Worker: {execution.workerId || 'Pending Claim'}
                  </span>
                  <span className="flex items-center gap-1">
                    <Clock className="w-3.5 h-3.5 text-slate-400" />
                    Queued: {new Date(execution.queuedAt).toLocaleTimeString()}
                  </span>
                  {execution.durationSeconds && (
                    <span className="font-semibold text-slate-700">
                      Duration: {execution.durationSeconds}s
                    </span>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-2">
                {(execution.status === 'Running' || execution.status === 'Queued') && (
                  <button
                    onClick={handleCancel}
                    disabled={cancelling}
                    className="px-3.5 py-2 bg-rose-600 hover:bg-rose-700 text-white text-xs font-semibold rounded-lg flex items-center gap-1.5 transition-colors disabled:opacity-50"
                  >
                    <StopCircle className="w-4 h-4" />
                    {cancelling ? 'Cancelling...' : 'Cancel Job'}
                  </button>
                )}

                {(execution.status === 'Failed' || execution.status === 'TimedOut') && (
                  <>
                    <button
                      onClick={handleRetry}
                      disabled={retrying}
                      className="px-3.5 py-2 bg-brand-600 hover:bg-brand-700 text-white text-xs font-semibold rounded-lg flex items-center gap-1.5 transition-colors disabled:opacity-50"
                    >
                      <RotateCcw className="w-4 h-4" />
                      {retrying ? 'Retrying...' : 'Retry Job'}
                    </button>

                    <button
                      onClick={() => setAiModalOpen(true)}
                      className="px-3.5 py-2 bg-linear-to-r from-purple-600 to-indigo-600 hover:from-purple-700 hover:to-indigo-700 text-white text-xs font-semibold rounded-lg flex items-center gap-1.5 shadow-xs transition-colors"
                    >
                      <Sparkles className="w-4 h-4 text-purple-200" />
                      {aiAnalysis ? 'View AI Diagnosis' : 'Analyze Failure'}
                    </button>
                  </>
                )}
              </div>
            </div>

            {/* Live Progress Bar */}
            <div>
              <ProgressBar progress={execution.progress} status={execution.status} />
            </div>

            {/* Error Banner if Failed */}
            {execution.errorMessage && (
              <div className="p-4 bg-rose-50 border border-rose-200 rounded-lg text-xs text-rose-800">
                <p className="font-semibold mb-1">Execution Failure Reason:</p>
                <p className="font-mono">{execution.errorMessage}</p>
              </div>
            )}
          </div>

          {/* Live Terminal Log Stream */}
          <div className="bg-slate-950 rounded-xl border border-slate-800 shadow-xl overflow-hidden flex flex-col h-[480px]">
            <div className="px-4 py-2.5 bg-slate-900 border-b border-slate-800 flex items-center justify-between text-xs text-slate-400">
              <div className="flex items-center gap-2 font-mono">
                <Terminal className="w-4 h-4 text-brand-400" />
                <span className="text-slate-300 font-semibold">Streaming Log Console</span>
                <span>({logs.length} entries)</span>
              </div>
              <div className="flex items-center gap-3 text-[11px]">
                <span className="flex items-center gap-1 text-slate-400">
                  <span className="w-2 h-2 rounded-full bg-emerald-500 animate-ping" />
                  Live Socket Stream
                </span>
              </div>
            </div>

            <div
              ref={logsTerminalRef}
              className="flex-1 p-4 font-mono text-xs overflow-y-auto space-y-1.5 text-slate-300 scrollbar-thin scrollbar-thumb-slate-800"
            >
              {logs.length === 0 ? (
                <div className="py-8 text-center text-slate-600">
                  Waiting for worker logs to stream...
                </div>
              ) : (
                logs.map((log) => {
                  const levelColor =
                    log.level === 'Error' || log.level === 'Critical'
                      ? 'text-rose-400 font-semibold'
                      : log.level === 'Warning'
                      ? 'text-amber-400'
                      : 'text-brand-300';

                  return (
                    <div key={log.sequenceNumber} className="flex items-start gap-2 leading-relaxed">
                      <span className="text-slate-600 shrink-0 select-none">
                        [{new Date(log.timestamp).toLocaleTimeString()}]
                      </span>
                      <span className={`shrink-0 uppercase text-[10px] px-1 rounded bg-slate-900 border border-slate-800 ${levelColor}`}>
                        {log.level}
                      </span>
                      <span className="text-slate-200 break-all">{log.message}</span>
                    </div>
                  );
                })
              )}
            </div>
          </div>
        </>
      )}

      {/* AI Diagnosis Modal */}
      {execution && (
        <AiAnalysisModal
          isOpen={aiModalOpen}
          onClose={() => setAiModalOpen(false)}
          executionId={execution.id}
          jobName={execution.jobName}
          analysis={aiAnalysis}
          onRetry={handleRetry}
        />
      )}
    </div>
  );
};
