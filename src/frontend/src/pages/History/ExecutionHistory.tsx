import React, { useState, useEffect } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import {
  RotateCcw,
  Sparkles,
  ExternalLink,
  ChevronLeft,
  ChevronRight
} from 'lucide-react';
import { api } from '../../services/api';
import { JobExecution, JobStatus, JobDefinition } from '../../types';
import { StatusBadge } from '../../components/common/StatusBadge';
import { AiAnalysisModal } from '../AiAnalysis/AiAnalysisModal';

export const ExecutionHistory: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();

  const statusParam = searchParams.get('status') as JobStatus | null;
  const jobIdParam = searchParams.get('jobId');

  const [executions, setExecutions] = useState<JobExecution[]>([]);
  const [jobs, setJobs] = useState<JobDefinition[]>([]);
  const [selectedJobId, setSelectedJobId] = useState<string>(jobIdParam || '');
  const [selectedStatus, setSelectedStatus] = useState<string>(statusParam || '');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);

  // AI Modal state
  const [selectedExecution, setSelectedExecution] = useState<JobExecution | null>(null);
  const [aiModalOpen, setAiModalOpen] = useState(false);

  useEffect(() => {
    async function loadJobs() {
      try {
        const res = await api.jobs.list({ pageSize: 100 });
        setJobs(res.items);
      } catch {
        // quiet
      }
    }
    loadJobs();
  }, []);

  const fetchHistory = async () => {
    setLoading(true);
    try {
      if (selectedJobId) {
        const res = await api.executions.listByJob(selectedJobId, {
          page,
          pageSize: 15,
          status: selectedStatus ? (selectedStatus as JobStatus) : undefined,
        });
        setExecutions(res.items);
        setTotalCount(res.totalCount);
      } else {
        // If no specific job, query active/recent or default to first job
        if (jobs.length > 0) {
          const res = await api.executions.listByJob(jobs[0].id, {
            page,
            pageSize: 15,
            status: selectedStatus ? (selectedStatus as JobStatus) : undefined,
          });
          setExecutions(res.items);
          setTotalCount(res.totalCount);
        }
      }
    } catch (err) {
      console.error('Failed to load execution history:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (jobs.length > 0) {
      if (!selectedJobId) {
        setSelectedJobId(jobs[0].id);
      }
      fetchHistory();
    }
  }, [jobs, selectedJobId, selectedStatus, page]);

  const handleFilterChange = (jobId: string, status: string) => {
    setSelectedJobId(jobId);
    setSelectedStatus(status);
    setPage(1);

    const params: Record<string, string> = {};
    if (jobId) params.jobId = jobId;
    if (status) params.status = status;
    setSearchParams(params);
  };

  const handleRetry = async (id: string) => {
    try {
      const newAttempt = await api.executions.retry(id);
      navigate(`/monitor?executionId=${newAttempt.id}`);
    } catch (err: unknown) {
      if (err instanceof Error) alert(err.message);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Execution History</h1>
        <p className="text-sm text-slate-500 mt-0.5">
          Audit trail of previous runs, error stack traces, and attempt lineages.
        </p>
      </div>

      {/* Filter Controls */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-wrap items-center gap-4">
        <div className="flex-1 min-w-[200px]">
          <label className="block text-[11px] font-semibold text-slate-500 uppercase mb-1">
            Filter by Job
          </label>
          <select
            value={selectedJobId}
            onChange={(e) => handleFilterChange(e.target.value, selectedStatus)}
            className="w-full text-xs border border-slate-300 rounded-lg px-3 py-2 bg-white focus:ring-2 focus:ring-brand-500"
          >
            {jobs.map((j) => (
              <option key={j.id} value={j.id}>
                {j.name}
              </option>
            ))}
          </select>
        </div>

        <div className="w-48">
          <label className="block text-[11px] font-semibold text-slate-500 uppercase mb-1">
            Status
          </label>
          <select
            value={selectedStatus}
            onChange={(e) => handleFilterChange(selectedJobId, e.target.value)}
            className="w-full text-xs border border-slate-300 rounded-lg px-3 py-2 bg-white focus:ring-2 focus:ring-brand-500"
          >
            <option value="">All Statuses</option>
            <option value="Completed">Completed</option>
            <option value="Failed">Failed</option>
            <option value="TimedOut">TimedOut</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        {loading ? (
          <div className="py-12 flex justify-center">
            <div className="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : executions.length === 0 ? (
          <div className="py-16 text-center text-slate-400">
            <p className="text-sm">No executions found for selected filters.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-6 py-3.5">Execution / Lineage</th>
                  <th className="px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5">Duration</th>
                  <th className="px-6 py-3.5">Queued / Completed</th>
                  <th className="px-6 py-3.5">Result / Error</th>
                  <th className="px-6 py-3.5 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {executions.map((e) => (
                  <tr key={e.id} className="hover:bg-slate-50/70 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900">{e.jobName}</div>
                      <div className="text-[11px] text-slate-400 font-mono mt-0.5">
                        Attempt #{e.attemptNumber}
                        {e.parentExecutionId && (
                          <span className="ml-1 text-brand-600 font-sans">
                            (Retried from {e.parentExecutionId.slice(0, 8)}...)
                          </span>
                        )}
                      </div>
                    </td>

                    <td className="px-6 py-4">
                      <StatusBadge status={e.status} size="sm" />
                    </td>

                    <td className="px-6 py-4 font-mono text-slate-600">
                      {e.durationSeconds !== undefined && e.durationSeconds !== null
                        ? `${e.durationSeconds}s`
                        : '—'}
                    </td>

                    <td className="px-6 py-4 text-slate-500">
                      <div>Q: {new Date(e.queuedAt).toLocaleTimeString()}</div>
                      {e.completedAt && (
                        <div className="text-slate-400 text-[10px]">
                          C: {new Date(e.completedAt).toLocaleTimeString()}
                        </div>
                      )}
                    </td>

                    <td className="px-6 py-4 max-w-xs">
                      {e.errorMessage ? (
                        <span className="text-rose-600 font-mono text-[11px] line-clamp-2">
                          {e.errorMessage}
                        </span>
                      ) : e.result ? (
                        <span className="text-slate-500 font-mono text-[11px] line-clamp-1">
                          {e.result}
                        </span>
                      ) : (
                        '—'
                      )}
                    </td>

                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => navigate(`/monitor?executionId=${e.id}`)}
                          className="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium rounded text-xs transition-colors flex items-center gap-1"
                          title="View Logs in Terminal"
                        >
                          Logs <ExternalLink className="w-3 h-3" />
                        </button>

                        {(e.status === 'Failed' || e.status === 'TimedOut') && (
                          <>
                            <button
                              onClick={() => {
                                setSelectedExecution(e);
                                setAiModalOpen(true);
                              }}
                              className="p-1.5 text-purple-600 hover:bg-purple-50 rounded-lg transition-colors"
                              title="AI Root Cause Analysis"
                            >
                              <Sparkles className="w-4 h-4" />
                            </button>

                            <button
                              onClick={() => handleRetry(e.id)}
                              className="p-1.5 text-brand-600 hover:bg-brand-50 rounded-lg transition-colors"
                              title="Retry Execution"
                            >
                              <RotateCcw className="w-4 h-4" />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        <div className="p-4 border-t border-slate-100 flex items-center justify-between text-xs text-slate-500">
          <span>
            Showing page {page} ({totalCount} total runs)
          </span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1 rounded border border-slate-200 disabled:opacity-40"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              onClick={() => setPage((p) => p + 1)}
              disabled={executions.length < 15}
              className="p-1 rounded border border-slate-200 disabled:opacity-40"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>

      {selectedExecution && (
        <AiAnalysisModal
          isOpen={aiModalOpen}
          onClose={() => setAiModalOpen(false)}
          executionId={selectedExecution.id}
          jobName={selectedExecution.jobName}
          analysis={null}
          onRetry={() => handleRetry(selectedExecution.id)}
        />
      )}
    </div>
  );
};
