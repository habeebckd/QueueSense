import React, { useState, useEffect } from 'react';
import {
  Plus,
  Clock,
  Trash2,
  CheckCircle,
  XCircle
} from 'lucide-react';
import { api } from '../../services/api';
import { JobSchedule, JobDefinition, ScheduleType } from '../../types';
import { Modal } from '../../components/common/Modal';

const CRON_PRESETS = [
  { label: 'Every 5 Minutes', value: '*/5 * * * *' },
  { label: 'Every 15 Minutes', value: '*/15 * * * *' },
  { label: 'Every Hour', value: '0 * * * *' },
  { label: 'Every 6 Hours', value: '0 */6 * * *' },
  { label: 'Daily at Midnight (00:00 UTC)', value: '0 0 * * *' },
];

export const ScheduleList: React.FC = () => {
  const [schedules, setSchedules] = useState<JobSchedule[]>([]);
  const [jobs, setJobs] = useState<JobDefinition[]>([]);
  const [loading, setLoading] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);

  // Form State
  const [selectedJobId, setSelectedJobId] = useState('');
  const [scheduleType, setScheduleType] = useState<ScheduleType>('RecurringCron');
  const [cronExpression, setCronExpression] = useState('0 */2 * * *');
  const [timeZone, setTimeZone] = useState('UTC');
  const [isEnabled, setIsEnabled] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchSchedulesAndJobs = async () => {
    setLoading(true);
    try {
      const [schedRes, jobsRes] = await Promise.all([
        api.schedules.list(),
        api.jobs.list({ pageSize: 100 }),
      ]);
      setSchedules(schedRes);
      setJobs(jobsRes.items);
      if (jobsRes.items.length > 0 && !selectedJobId) {
        setSelectedJobId(jobsRes.items[0].id);
      }
    } catch (err) {
      console.error('Failed to load schedules:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSchedulesAndJobs();
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await api.schedules.create({
        jobDefinitionId: selectedJobId,
        scheduleType,
        cronExpression,
        timeZone,
        isEnabled,
      });
      setModalOpen(false);
      fetchSchedulesAndJobs();
    } catch (err: unknown) {
      if (err instanceof Error) setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (confirm('Are you sure you want to delete this schedule?')) {
      try {
        await api.schedules.delete(id);
        fetchSchedulesAndJobs();
      } catch (err: unknown) {
        if (err instanceof Error) alert(err.message);
      }
    }
  };

  const handleToggle = async (schedule: JobSchedule) => {
    try {
      await api.schedules.update(schedule.id, {
        ...schedule,
        isEnabled: !schedule.isEnabled,
      });
      fetchSchedulesAndJobs();
    } catch (err: unknown) {
      if (err instanceof Error) alert(err.message);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Recurring Schedules</h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Configure automated cron triggers and execution cadences for jobs.
          </p>
        </div>
        <button
          onClick={() => setModalOpen(true)}
          className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-sm font-medium shadow-xs transition-colors flex items-center gap-2 self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          Add Schedule
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-xs overflow-hidden">
        {loading ? (
          <div className="py-12 flex justify-center">
            <div className="w-6 h-6 border-2 border-brand-500 border-t-transparent rounded-full animate-spin" />
          </div>
        ) : schedules.length === 0 ? (
          <div className="py-16 text-center text-slate-400">
            <p className="text-sm">No schedules configured yet.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 font-semibold uppercase tracking-wider border-b border-slate-200">
                <tr>
                  <th className="px-6 py-3.5">Job Target</th>
                  <th className="px-6 py-3.5">Schedule Type</th>
                  <th className="px-6 py-3.5">Cron Expression</th>
                  <th className="px-6 py-3.5">Next Run (UTC)</th>
                  <th className="px-6 py-3.5">Active</th>
                  <th className="px-6 py-3.5 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {schedules.map((s) => (
                  <tr key={s.id} className="hover:bg-slate-50/70 transition-colors">
                    <td className="px-6 py-4 font-semibold text-slate-900 text-sm">
                      {s.jobName || 'Unnamed Job'}
                    </td>

                    <td className="px-6 py-4">
                      <span className="px-2 py-0.5 rounded bg-slate-100 text-slate-700 font-mono text-[11px]">
                        {s.scheduleType}
                      </span>
                    </td>

                    <td className="px-6 py-4 font-mono text-xs text-brand-700 font-semibold">
                      {s.cronExpression || 'Immediate'}
                    </td>

                    <td className="px-6 py-4 text-slate-600">
                      {s.nextRunAt ? (
                        <span className="flex items-center gap-1.5">
                          <Clock className="w-3.5 h-3.5 text-slate-400" />
                          {new Date(s.nextRunAt).toLocaleString()}
                        </span>
                      ) : (
                        '—'
                      )}
                    </td>

                    <td className="px-6 py-4">
                      <button
                        onClick={() => handleToggle(s)}
                        className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium border ${
                          s.isEnabled
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                            : 'bg-slate-100 text-slate-500 border-slate-200'
                        }`}
                      >
                        {s.isEnabled ? (
                          <>
                            <CheckCircle className="w-3 h-3 text-emerald-600" />
                            Enabled
                          </>
                        ) : (
                          <>
                            <XCircle className="w-3 h-3 text-slate-400" />
                            Disabled
                          </>
                        )}
                      </button>
                    </td>

                    <td className="px-6 py-4 text-right">
                      <button
                        onClick={() => handleDelete(s.id)}
                        className="p-1.5 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
                        title="Delete Schedule"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create Schedule Modal */}
      <Modal isOpen={modalOpen} onClose={() => setModalOpen(false)} title="Add Job Schedule" maxWidth="md">
        <form onSubmit={handleCreate} className="space-y-4">
          {error && (
            <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-lg">
              {error}
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
              Select Job *
            </label>
            <select
              value={selectedJobId}
              onChange={(e) => setSelectedJobId(e.target.value)}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white"
            >
              {jobs.map((j) => (
                <option key={j.id} value={j.id}>
                  {j.name} ({j.handlerType})
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
              Schedule Type *
            </label>
            <select
              value={scheduleType}
              onChange={(e) => setScheduleType(e.target.value as ScheduleType)}
              className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white"
            >
              <option value="RecurringCron">Recurring Cron (Standard 5-Field)</option>
              <option value="Immediate">Immediate One-Time</option>
            </select>
          </div>

          {scheduleType === 'RecurringCron' && (
            <div>
              <div className="flex items-center justify-between mb-1">
                <label className="block text-xs font-semibold text-slate-700 uppercase">
                  Cron Expression (UTC) *
                </label>
                <div className="flex gap-1">
                  {CRON_PRESETS.slice(0, 3).map((p) => (
                    <button
                      key={p.value}
                      type="button"
                      onClick={() => setCronExpression(p.value)}
                      className="text-[10px] bg-slate-100 hover:bg-slate-200 text-slate-700 px-1.5 py-0.5 rounded"
                    >
                      {p.label}
                    </button>
                  ))}
                </div>
              </div>
              <input
                type="text"
                required
                value={cronExpression}
                onChange={(e) => setCronExpression(e.target.value)}
                className="w-full font-mono text-sm px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500"
                placeholder="*/5 * * * *"
              />
              <p className="text-[11px] text-slate-400 mt-1">
                Format: minute hour day-of-month month day-of-week
              </p>
            </div>
          )}

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
              Time Zone
            </label>
            <input
              type="text"
              value={timeZone}
              onChange={(e) => setTimeZone(e.target.value)}
              className="w-full text-sm px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500"
              placeholder="UTC"
            />
          </div>

          <div className="flex items-center gap-2 pt-2">
            <input
              type="checkbox"
              id="schedule-enabled"
              checked={isEnabled}
              onChange={(e) => setIsEnabled(e.target.checked)}
              className="w-4 h-4 text-brand-600 rounded border-slate-300"
            />
            <label htmlFor="schedule-enabled" className="text-sm font-medium text-slate-700">
              Enable schedule immediately
            </label>
          </div>

          <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
            <button
              type="button"
              onClick={() => setModalOpen(false)}
              className="px-4 py-2 border border-slate-300 hover:bg-slate-50 text-slate-700 text-sm font-medium rounded-lg"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={submitting}
              className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white text-sm font-medium rounded-lg shadow-sm disabled:opacity-50"
            >
              {submitting ? 'Creating...' : 'Save Schedule'}
            </button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
