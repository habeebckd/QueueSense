import React from 'react';
import { JobStatus } from '../../types';

interface ProgressBarProps {
  progress: number;
  status?: JobStatus;
  showLabel?: boolean;
}

export const ProgressBar: React.FC<ProgressBarProps> = ({
  progress,
  status = 'Running',
  showLabel = true,
}) => {
  const clamped = Math.min(100, Math.max(0, progress));

  const getColor = () => {
    switch (status) {
      case 'Completed':
        return 'bg-emerald-500';
      case 'Failed':
        return 'bg-rose-500';
      case 'TimedOut':
        return 'bg-orange-500';
      case 'Cancelled':
        return 'bg-slate-400';
      default:
        return 'bg-blue-600';
    }
  };

  return (
    <div className="w-full">
      {showLabel && (
        <div className="flex justify-between items-center text-xs font-medium text-slate-600 mb-1">
          <span>Progress</span>
          <span>{clamped}%</span>
        </div>
      )}
      <div className="w-full bg-slate-200 rounded-full h-2 overflow-hidden">
        <div
          className={`h-full transition-all duration-300 ease-out rounded-full ${getColor()}`}
          style={{ width: `${clamped}%` }}
        />
      </div>
    </div>
  );
};
