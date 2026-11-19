import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Layers,
  Activity,
  CheckCircle2,
  XCircle,
  TrendingUp,
  Clock,
  Play,
  ArrowRight,
  Sparkles
} from 'lucide-react';
import {
  ResponsiveContainer,
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip
} from 'recharts';
import { api } from '../../services/api';
import { signalRService } from '../../services/signalr';
import { DashboardSummary, MetricPoint, JobExecution } from '../../types';
import { StatusBadge } from '../../components/common/StatusBadge';
import { ProgressBar } from '../../components/common/ProgressBar';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [metrics, setMetrics] = useState<MetricPoint[]>([]);
  const [activeExecutions, setActiveExecutions] = useState<JobExecution[]>([]);
  const [loading, setLoading] = useState(true);

  const loadData = async () => {
    try {
      const [sumData, metData, activeData] = await Promise.all([
        api.dashboard.getSummary(),
        api.dashboard.getMetrics('24h'),
        api.executions.getActive(),
      ]);
      setSummary(sumData);
      setMetrics(metData);
      setActiveExecutions(activeData);
    } catch (err) {
      console.error('Error fetching dashboard data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadData();

    // Listen to real-time events to dynamically update dashboard counters
    const unsubProgress = signalRService.onJobProgress((p) => {
      setActiveExecutions((prev) =>
        prev.map((e) => (e.id === p.executionId ? { ...e, progress: p.progressPercentage } : e))
      );
    });

    const unsubCompleted = signalRService.onJobCompleted(() => {
      loadData();
    });

    const unsubFailed = signalRService.onJobFailed(() => {
      loadData();
    });

    const unsubCancelled = signalRService.onJobCancelled(() => {
      loadData();
    });

    const interval = setInterval(loadData, 10000);

    return () => {
      unsubProgress();
      unsubCompleted();
      unsubFailed();
      unsubCancelled();
      clearInterval(interval);
    };
  }, []);

  if (loading && !summary) {
    return (
      <div className="flex items-center justify-center min-h-[60vh]">
        <div className="w-8 h-8 border-4 border-brand-500 border-t-transparent rounded-full animate-spin" />
      </div>
    );
  }

  const kpis = [
    {
      label: 'Total Jobs Configured',
      value: summary?.totalJobs ?? 0,
      sub: `${summary?.activeJobs ?? 0} active & scheduled`,
      icon: Layers,
      color: 'text-slate-700',
      bg: 'bg-slate-100',
    },
    {
      label: 'Currently Running',
      value: summary?.runningExecutions ?? 0,
      sub: `${summary?.queuedExecutions ?? 0} queued in queue`,
      icon: Activity,
      color: 'text-blue-600',
      bg: 'bg-blue-50',
      pulse: (summary?.runningExecutions ?? 0) > 0,
    },
    {
      label: 'Completed Runs',
      value: summary?.completedExecutions ?? 0,
      sub: 'All-time successful batches',
      icon: CheckCircle2,
      color: 'text-emerald-600',
      bg: 'bg-emerald-50',
    },
    {
      label: 'Failed Executions',
      value: summary?.failedExecutions ?? 0,
      sub: 'AI Diagnosed and analyzed',
      icon: XCircle,
      color: 'text-rose-600',
      bg: 'bg-rose-50',
    },
    {
      label: 'Success Rate',
      value: `${summary?.successRatePercentage ?? 100}%`,
      sub: 'Reliability percentage',
      icon: TrendingUp,
      color: 'text-indigo-600',
      bg: 'bg-indigo-50',
    },
    {
      label: 'Average Duration',
      value: `${summary?.averageDurationSeconds ?? 0}s`,
      sub: 'Mean execution latency',
      icon: Clock,
      color: 'text-amber-600',
      bg: 'bg-amber-50',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">System Overview</h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Real-time status of scheduled background jobs, durable worker leases, and AI insights.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => navigate('/jobs')}
            className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-sm font-medium shadow-xs transition-colors flex items-center gap-2"
          >
            <Play className="w-4 h-4" />
            Manage Jobs
          </button>
        </div>
      </div>

      {/* KPI Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-6 gap-4">
        {kpis.map((kpi, index) => {
          const Icon = kpi.icon;
          return (
            <div
              key={index}
              className="bg-white p-4 rounded-xl border border-slate-200 shadow-xs flex flex-col justify-between"
            >
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500 uppercase tracking-wider">
                  {kpi.label}
                </span>
                <div className={`p-2 rounded-lg ${kpi.bg}`}>
                  <Icon className={`w-4 h-4 ${kpi.color} ${kpi.pulse ? 'animate-pulse' : ''}`} />
                </div>
              </div>
              <div>
                <span className="text-2xl font-bold text-slate-900 tracking-tight">{kpi.value}</span>
                <p className="text-[11px] text-slate-400 mt-0.5">{kpi.sub}</p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Analytics Chart & AI Banner */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 bg-white p-6 rounded-xl border border-slate-200 shadow-xs">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="font-semibold text-slate-800">Execution Throughput (24 Hours)</h3>
              <p className="text-xs text-slate-400">Total jobs executed categorized by success and failure</p>
            </div>
            <span className="text-xs font-medium px-2.5 py-1 bg-slate-100 text-slate-600 rounded-md">
              Past 24 Hours
            </span>
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={metrics} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                <defs>
                  <linearGradient id="successGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10b981" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#10b981" stopOpacity={0.0} />
                  </linearGradient>
                  <linearGradient id="failureGrad" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#f43f5e" stopOpacity={0.4} />
                    <stop offset="95%" stopColor="#f43f5e" stopOpacity={0.0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                <XAxis dataKey="timestamp" stroke="#94a3b8" fontSize={11} tickLine={false} />
                <YAxis stroke="#94a3b8" fontSize={11} tickLine={false} />
                <Tooltip
                  contentStyle={{
                    backgroundColor: '#ffffff',
                    borderRadius: '8px',
                    boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)',
                    border: '1px solid #e2e8f0',
                    fontSize: '12px',
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="successCount"
                  name="Successful"
                  stroke="#10b981"
                  strokeWidth={2}
                  fillOpacity={1}
                  fill="url(#successGrad)"
                />
                <Area
                  type="monotone"
                  dataKey="failureCount"
                  name="Failed"
                  stroke="#f43f5e"
                  strokeWidth={2}
                  fillOpacity={1}
                  fill="url(#failureGrad)"
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* AI Diagnostics Card */}
        <div className="bg-linear-to-br from-slate-900 to-slate-800 text-white p-6 rounded-xl shadow-xs flex flex-col justify-between">
          <div>
            <div className="flex items-center gap-2 text-brand-400 font-semibold text-sm mb-3">
              <Sparkles className="w-5 h-5" />
              <span>AI Troubleshooting Engine</span>
            </div>
            <h3 className="text-xl font-bold mb-2">Automated Failure Analysis</h3>
            <p className="text-xs text-slate-300 leading-relaxed mb-4">
              When jobs encounter timeouts, memory exhaustion, or network drops, QueueSense automatically sanitizes execution telemetry and analyzes root causes.
            </p>
            <div className="space-y-2 text-xs">
              <div className="flex items-center gap-2 bg-slate-800/80 p-2.5 rounded-lg border border-slate-700">
                <span className="w-2 h-2 rounded-full bg-emerald-400" />
                <span>Deterministic Diagnostic Heuristics</span>
              </div>
              <div className="flex items-center gap-2 bg-slate-800/80 p-2.5 rounded-lg border border-slate-700">
                <span className="w-2 h-2 rounded-full bg-brand-400" />
                <span>Sensitive PII & Credential Sanitizer</span>
              </div>
              <div className="flex items-center gap-2 bg-slate-800/80 p-2.5 rounded-lg border border-slate-700">
                <span className="w-2 h-2 rounded-full bg-indigo-400" />
                <span>One-Click Retry with Lineage Link</span>
              </div>
            </div>
          </div>

          <button
            onClick={() => navigate('/history?status=Failed')}
            className="w-full mt-6 py-2 px-3 bg-brand-600 hover:bg-brand-500 text-white rounded-lg text-xs font-semibold flex items-center justify-center gap-1.5 transition-colors"
          >
            Review Failure Diagnostics <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Active Executions */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        <div className="p-4 border-b border-slate-100 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <h3 className="font-semibold text-slate-800">Active Executions</h3>
            <span className="text-xs px-2 py-0.5 rounded-full bg-blue-50 text-blue-700 font-medium border border-blue-200">
              {activeExecutions.length} live
            </span>
          </div>
          <button
            onClick={() => navigate('/monitor')}
            className="text-xs text-brand-600 hover:text-brand-800 font-semibold flex items-center gap-1"
          >
            Open Live Terminal <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>

        {activeExecutions.length === 0 ? (
          <div className="p-8 text-center text-slate-400">
            <p className="text-sm">No jobs currently executing.</p>
            <button
              onClick={() => navigate('/jobs')}
              className="mt-2 text-xs text-brand-600 hover:underline font-medium"
            >
              Trigger a sample job
            </button>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-100">
                <tr>
                  <th className="px-4 py-3">Job Name</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 w-48">Progress</th>
                  <th className="px-4 py-3">Worker Lease</th>
                  <th className="px-4 py-3">Queued At</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {activeExecutions.map((e) => (
                  <tr key={e.id} className="hover:bg-slate-50/60 transition-colors">
                    <td className="px-4 py-3 font-semibold text-slate-800">{e.jobName}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={e.status} size="sm" />
                    </td>
                    <td className="px-4 py-3">
                      <ProgressBar progress={e.progress} status={e.status} showLabel={false} />
                    </td>
                    <td className="px-4 py-3 font-mono text-[11px] text-slate-500">
                      {e.workerId || 'Pending Claim'}
                    </td>
                    <td className="px-4 py-3 text-slate-400">
                      {new Date(e.queuedAt).toLocaleTimeString()}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => navigate(`/monitor?executionId=${e.id}`)}
                        className="px-2.5 py-1 bg-slate-100 hover:bg-slate-200 text-slate-700 font-medium rounded text-xs transition-colors"
                      >
                        Monitor
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
