import React, { useState } from 'react';
import {
  Sparkles,
  AlertOctagon,
  CheckCircle2,
  RotateCcw,
  ShieldAlert,
  HelpCircle
} from 'lucide-react';
import { AiAnalysisResult } from '../../types';
import { api } from '../../services/api';
import { Modal } from '../../components/common/Modal';

interface AiAnalysisModalProps {
  isOpen: boolean;
  onClose: () => void;
  executionId: string;
  jobName: string;
  analysis: AiAnalysisResult | null;
  onRetry: () => void;
}

export const AiAnalysisModal: React.FC<AiAnalysisModalProps> = ({
  isOpen,
  onClose,
  executionId,
  jobName,
  analysis: initialAnalysis,
  onRetry,
}) => {
  const [analysis, setAnalysis] = useState<AiAnalysisResult | null>(initialAnalysis);
  const [loading, setLoading] = useState(false);

  const triggerAnalysis = async () => {
    setLoading(true);
    try {
      await api.ai.triggerAnalysis(executionId);
      const updated = await api.ai.getAnalysis(executionId);
      setAnalysis(updated);
    } catch (err: unknown) {
      if (err instanceof Error) alert(err.message);
    } finally {
      setLoading(false);
    }
  };

  const currentAnalysis = analysis || initialAnalysis;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="AI Failure Root Cause & Troubleshooting"
      maxWidth="2xl"
    >
      <div className="space-y-6">
        <div className="flex items-center justify-between pb-4 border-b border-slate-100">
          <div>
            <h4 className="text-sm font-semibold text-slate-800">{jobName}</h4>
            <p className="text-xs text-slate-400 font-mono">Execution ID: {executionId}</p>
          </div>
          {currentAnalysis && (
            <div className="flex items-center gap-2">
              <span className="text-xs font-semibold px-2.5 py-1 rounded-full bg-purple-50 text-purple-700 border border-purple-200">
                Confidence: {currentAnalysis.confidence}
              </span>
              <span
                className={`text-xs font-semibold px-2.5 py-1 rounded-full border ${
                  currentAnalysis.severity === 'Critical' || currentAnalysis.severity === 'High'
                    ? 'bg-rose-50 text-rose-700 border-rose-200'
                    : 'bg-amber-50 text-amber-700 border-amber-200'
                }`}
              >
                Severity: {currentAnalysis.severity}
              </span>
            </div>
          )}
        </div>

        {!currentAnalysis ? (
          <div className="py-8 text-center space-y-4">
            <Sparkles className="w-8 h-8 text-purple-500 mx-auto animate-bounce" />
            <p className="text-sm text-slate-600">
              No AI failure diagnosis recorded yet for this execution.
            </p>
            <button
              onClick={triggerAnalysis}
              disabled={loading}
              className="px-4 py-2 bg-purple-600 hover:bg-purple-700 text-white rounded-lg text-xs font-semibold shadow-xs disabled:opacity-50"
            >
              {loading ? 'Analyzing with AI...' : 'Run Failure Analysis Now'}
            </button>
          </div>
        ) : (
          <>
            {/* Summary */}
            <div className="p-4 bg-slate-50 border border-slate-200 rounded-xl">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-1">
                Diagnostic Summary
              </p>
              <p className="text-sm font-medium text-slate-900 leading-relaxed">
                {currentAnalysis.summary}
              </p>
            </div>

            {/* Probable Causes */}
            <div>
              <h5 className="text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                <AlertOctagon className="w-4 h-4 text-rose-500" />
                Probable Root Causes
              </h5>
              <div className="space-y-2">
                {currentAnalysis.probableCauses.map((cause, i) => (
                  <div
                    key={i}
                    className="p-3 bg-white border border-slate-200 rounded-lg text-xs text-slate-700 flex items-start gap-2 shadow-2xs"
                  >
                    <span className="w-4 h-4 rounded-full bg-rose-100 text-rose-700 font-bold flex items-center justify-center shrink-0 text-[10px]">
                      {i + 1}
                    </span>
                    <span>{cause}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Suggested Solutions */}
            <div>
              <h5 className="text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2 flex items-center gap-1.5">
                <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                Recommended Troubleshooting Actions
              </h5>
              <div className="space-y-2">
                {currentAnalysis.suggestedSolutions.map((sol, i) => (
                  <div
                    key={i}
                    className="p-3 bg-white border border-slate-200 rounded-lg text-xs text-slate-700 flex items-start gap-2 shadow-2xs"
                  >
                    <span className="w-4 h-4 rounded-full bg-emerald-100 text-emerald-700 font-bold flex items-center justify-center shrink-0 text-[10px]">
                      {i + 1}
                    </span>
                    <span>{sol}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Safety Guardrail Alert */}
            <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg text-xs text-amber-800 flex items-start gap-2">
              <ShieldAlert className="w-4 h-4 text-amber-600 shrink-0 mt-0.5" />
              <span>
                <strong>Advisory Notice:</strong> AI suggestions are purely diagnostic recommendations for engineers. AI cannot execute database mutations or destructive actions directly.
              </span>
            </div>

            {/* Actions */}
            <div className="flex items-center justify-between pt-4 border-t border-slate-100">
              <div className="flex items-center gap-1 text-xs text-slate-400">
                <HelpCircle className="w-3.5 h-3.5" />
                <span>Retry recommended: {currentAnalysis.retryRecommended ? 'Yes' : 'No'}</span>
              </div>

              <div className="flex items-center gap-3">
                <button
                  onClick={onClose}
                  className="px-4 py-2 border border-slate-300 hover:bg-slate-50 text-slate-700 text-xs font-medium rounded-lg"
                >
                  Close
                </button>
                {currentAnalysis.retryRecommended && (
                  <button
                    onClick={() => {
                      onClose();
                      onRetry();
                    }}
                    className="px-4 py-2 bg-brand-600 hover:bg-brand-700 text-white text-xs font-semibold rounded-lg flex items-center gap-1.5 shadow-sm"
                  >
                    <RotateCcw className="w-3.5 h-3.5" />
                    Retry Execution
                  </button>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </Modal>
  );
};
