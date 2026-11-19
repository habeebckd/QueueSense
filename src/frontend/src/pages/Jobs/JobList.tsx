import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Plus,
  Search,
  Play,
  Edit2,
  Trash2,
  History,
  CheckCircle,
  XCircle,
  Clock
} from 'lucide-react';
import { api } from '../../services/api';
import { JobDefinition } from '../../types';
import { StatusBadge } from '../../components/common/StatusBadge';
import { JobModal } from './JobModal';

export const JobList: React.FC = () => {
  const navigate = useNavigate();
  const [jobs, setJobs] = useState<JobDefinition[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [selectedJob, setSelectedJob] = useState<JobDefinition | null>(null);
  const [runningJobId, setRunningJobId] = useState<string | null>(null);

  const fetchJobs = async () => {
    setLoading(true);
    try {
      const result = await api.jobs.list({ page, pageSize: 10, search });
      setJobs(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      console.error('Failed to load jobs:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchJobs();
  }, [page, search]);

  const handleRunNow = async (job: JobDefinition) => {
    setRunningJobId(job.id);
    try {
      const execution = await api.jobs.run(job.id);
      navigate(`/monitor?executionId=${execution.id}`);
    } catch (err: unknown) {
      if (err instanceof Error) {
        alert(err.message);
      }
    } finally {
      setRunningJobId(null);
    }
  };

  const handleDelete = async (id: string, name: string) => {
    if (confirm(`Are you sure you want to delete job '${name}'? This will remove all associated schedules.`)) {
      try {
        await api.jobs.delete(id);
        fetchJobs();
      } catch (err: unknown) {
        if (err instanceof Error) alert(err.message);
      }
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Job Definitions</h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Configure, schedule, and execute allowlisted background tasks.
          </p>
        </div>
        <button
          onClick={() => {
            setSelectedJob(null);
            setModalOpen(true);
          }}
          className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-sm font-medium shadow-xs transition-colors flex items-center gap-2 self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          Create Job
        </button>
      </div>

      {/* Filter & Search Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-col sm:flex-row items-center gap-4">
        <div className="relative flex-1 w-full">
          <Search className="w-4 h-4 absolute left-3 top-3 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Search by job name or description..."
            className="w-full pl-9 pr-4 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-brand-500"
          />
        </div>
        <div className="text-xs text-slate-500 shrink-0">
          Total: <span className="font-semibold text-slate-800">{totalCount}</span> registered jobs
        </div>
      </div>

      {/* Job Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        {loading ? (
          <div className="py-12 flex justify-center">
            <div className="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : jobs.length === 0 ? (
          <div className="py-16 text-center text-slate-400">
            <p className="text-sm">No job definitions found.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-6 py-3.5">Name & Handler</th>
                  <th className="px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5">Last Run Status</th>
                  <th className="px-6 py-3.5">Last Executed</th>
                  <th className="px-6 py-3.5 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {jobs.map((job) => (
                  <tr key={job.id} className="hover:bg-slate-50/70 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-slate-900 text-sm">{job.name}</div>
                      <div className="text-xs text-slate-400 font-mono mt-0.5">{job.handlerType}</div>
                      {job.description && (
                        <div className="text-xs text-slate-500 mt-1 max-w-md line-clamp-1">
                          {job.description}
                        </div>
                      )}
                    </td>

                    <td className="px-6 py-4">
                      {job.isEnabled ? (
                        <span className="inline-flex items-center gap-1 text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full text-[11px] font-medium border border-emerald-200">
                          <CheckCircle className="w-3 h-3 text-emerald-600" />
                          Enabled
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-slate-600 bg-slate-100 px-2.5 py-0.5 rounded-full text-[11px] font-medium border border-slate-200">
                          <XCircle className="w-3 h-3 text-slate-400" />
                          Disabled
                        </span>
                      )}
                    </td>

                    <td className="px-6 py-4">
                      {job.lastExecutionStatus ? (
                        <StatusBadge status={job.lastExecutionStatus} size="sm" />
                      ) : (
                        <span className="text-slate-400 text-xs">Never executed</span>
                      )}
                    </td>

                    <td className="px-6 py-4 text-slate-500">
                      {job.lastRunAt ? (
                        <span className="flex items-center gap-1">
                          <Clock className="w-3.5 h-3.5 text-slate-400" />
                          {new Date(job.lastRunAt).toLocaleString()}
                        </span>
                      ) : (
                        '—'
                      )}
                    </td>

                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleRunNow(job)}
                          disabled={!job.isEnabled || runningJobId === job.id}
                          className="px-3 py-1.5 bg-emerald-600 hover:bg-emerald-700 text-white font-medium rounded-lg text-xs flex items-center gap-1.5 transition-colors disabled:opacity-40"
                          title="Run Now"
                        >
                          <Play className="w-3.5 h-3.5" />
                          {runningJobId === job.id ? 'Starting...' : 'Run Now'}
                        </button>

                        <button
                          onClick={() => navigate(`/history?jobId=${job.id}`)}
                          className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-colors"
                          title="View Execution History"
                        >
                          <History className="w-4 h-4" />
                        </button>

                        <button
                          onClick={() => {
                            setSelectedJob(job);
                            setModalOpen(true);
                          }}
                          className="p-1.5 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-colors"
                          title="Edit Job"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>

                        <button
                          onClick={() => handleDelete(job.id, job.name)}
                          className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                          title="Delete Job"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <JobModal
        isOpen={modalOpen}
        onClose={() => setModalOpen(false)}
        jobToEdit={selectedJob}
        onSuccess={fetchJobs}
      />
    </div>
  );
};
