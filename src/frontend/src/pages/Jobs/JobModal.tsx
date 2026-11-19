import React, { useState, useEffect } from 'react';
import { JobDefinition } from '../../types';
import { api } from '../../services/api';
import { Modal } from '../../components/common/Modal';

interface JobModalProps {
  isOpen: boolean;
  onClose: () => void;
  jobToEdit?: JobDefinition | null;
  onSuccess: () => void;
}

const ALLOWED_HANDLERS = [
  {
    type: 'SampleDataProcessingJobHandler',
    label: 'Sample Data Processing (ETL Ingestion)',
    defaultConfig: '{\n  "batchSize": 100,\n  "dataSource": "external-crm"\n}',
  },
  {
    type: 'BatchRecordProcessingJobHandler',
    label: 'Batch Record Processing (High Throughput)',
    defaultConfig: '{\n  "totalRecords": 200,\n  "chunkSize": 40\n}',
  },
  {
    type: 'ReportGenerationJobHandler',
    label: 'Report Generation (PDF/Data Export)',
    defaultConfig: '{\n  "format": "PDF",\n  "includeCharts": true\n}',
  },
  {
    type: 'SimulatedFailureJobHandler',
    label: 'Simulated Failure Testing (AI Diagnosis Test)',
    defaultConfig: '{\n  "failureScenario": "DatabaseConnectionTimeout",\n  "failAfterSeconds": 2\n}',
  },
  {
    type: 'ConfigurableLongRunningJobHandler',
    label: 'Configurable Long-Running (Cancellation Test)',
    defaultConfig: '{\n  "durationSeconds": 40,\n  "intervalSeconds": 2\n}',
  },
];

export const JobModal: React.FC<JobModalProps> = ({
  isOpen,
  onClose,
  jobToEdit,
  onSuccess,
}) => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [handlerType, setHandlerType] = useState(ALLOWED_HANDLERS[0].type);
  const [configuration, setConfiguration] = useState(ALLOWED_HANDLERS[0].defaultConfig);
  const [isEnabled, setIsEnabled] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (jobToEdit) {
      setName(jobToEdit.name);
      setDescription(jobToEdit.description || '');
      setHandlerType(jobToEdit.handlerType);
      setConfiguration(jobToEdit.configuration || '{}');
      setIsEnabled(jobToEdit.isEnabled);
    } else {
      setName('');
      setDescription('');
      setHandlerType(ALLOWED_HANDLERS[0].type);
      setConfiguration(ALLOWED_HANDLERS[0].defaultConfig);
      setIsEnabled(true);
    }
    setError(null);
  }, [jobToEdit, isOpen]);

  const handleHandlerChange = (newType: string) => {
    setHandlerType(newType);
    if (!jobToEdit) {
      const template = ALLOWED_HANDLERS.find((h) => h.type === newType);
      if (template) setConfiguration(template.defaultConfig);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      if (jobToEdit) {
        await api.jobs.update(jobToEdit.id, {
          name,
          description,
          handlerType,
          configuration,
          isEnabled,
        });
      } else {
        await api.jobs.create({
          name,
          description,
          handlerType,
          configuration,
          isEnabled,
        });
      }
      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Failed to save job definition');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={jobToEdit ? 'Edit Background Job' : 'Create Background Job'}
      maxWidth="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 bg-rose-50 border border-rose-200 text-rose-700 text-xs rounded-lg">
            {error}
          </div>
        )}

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
            Job Name *
          </label>
          <input
            type="text"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500"
            placeholder="e.g. Daily Inventory Reconciliation"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
            Description
          </label>
          <textarea
            rows={2}
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500"
            placeholder="Describe the workload responsibility..."
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-slate-700 uppercase mb-1">
            Allowlisted Job Handler Type *
          </label>
          <select
            value={handlerType}
            onChange={(e) => handleHandlerChange(e.target.value)}
            className="w-full px-3 py-2 text-sm border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500 bg-white"
          >
            {ALLOWED_HANDLERS.map((h) => (
              <option key={h.type} value={h.type}>
                {h.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <div className="flex items-center justify-between mb-1">
            <label className="block text-xs font-semibold text-slate-700 uppercase">
              Configuration Parameters (JSON)
            </label>
            <button
              type="button"
              onClick={() => {
                const template = ALLOWED_HANDLERS.find((h) => h.type === handlerType);
                if (template) setConfiguration(template.defaultConfig);
              }}
              className="text-[11px] text-brand-600 hover:underline"
            >
              Reset Template
            </button>
          </div>
          <textarea
            rows={4}
            value={configuration}
            onChange={(e) => setConfiguration(e.target.value)}
            className="w-full font-mono text-xs px-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-brand-500 bg-slate-50"
          />
        </div>

        <div className="flex items-center gap-2 pt-2">
          <input
            type="checkbox"
            id="job-enabled"
            checked={isEnabled}
            onChange={(e) => setIsEnabled(e.target.checked)}
            className="w-4 h-4 text-brand-600 rounded border-slate-300 focus:ring-brand-500"
          />
          <label htmlFor="job-enabled" className="text-sm font-medium text-slate-700">
            Enable Job Scheduling & Triggering
          </label>
        </div>

        <div className="flex items-center justify-end gap-3 pt-4 border-t border-slate-100">
          <button
            type="button"
            onClick={onClose}
            className="px-4 py-2 border border-slate-300 hover:bg-slate-50 text-slate-700 text-sm font-medium rounded-lg"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={loading}
            className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white text-sm font-medium rounded-lg shadow-sm disabled:opacity-50"
          >
            {loading ? 'Saving...' : jobToEdit ? 'Save Changes' : 'Create Job'}
          </button>
        </div>
      </form>
    </Modal>
  );
};
