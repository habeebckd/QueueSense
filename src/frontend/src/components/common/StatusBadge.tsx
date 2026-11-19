import React from 'react';
import { JobStatus } from '../../types';

interface StatusBadgeProps {
  status: JobStatus | string;
  size?: 'sm' | 'md' | 'lg';
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, size = 'md' }) => {
  const getBadgeStyle = (s: string) => {
    switch (s) {
      case 'Completed':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200 ring-emerald-500/20';
      case 'Running':
        return 'bg-blue-50 text-blue-700 border-blue-200 ring-blue-500/20 animate-pulse';
      case 'Queued':
        return 'bg-amber-50 text-amber-700 border-amber-200 ring-amber-500/20';
      case 'Failed':
        return 'bg-rose-50 text-rose-700 border-rose-200 ring-rose-500/20';
      case 'TimedOut':
        return 'bg-orange-50 text-orange-700 border-orange-200 ring-orange-500/20';
      case 'CancellationRequested':
        return 'bg-purple-50 text-purple-700 border-purple-200 ring-purple-500/20';
      case 'Cancelled':
        return 'bg-slate-100 text-slate-600 border-slate-200 ring-slate-500/20';
      default:
        return 'bg-slate-50 text-slate-600 border-slate-200';
    }
  };

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-1 text-xs font-medium',
    lg: 'px-3 py-1.5 text-sm font-semibold',
  }[size];

  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border shadow-xs ${sizeClasses} ${getBadgeStyle(
        status
      )}`}
    >
      <span
        className={`w-1.5 h-1.5 rounded-full ${
          status === 'Running'
            ? 'bg-blue-600 animate-ping'
            : status === 'Completed'
            ? 'bg-emerald-600'
            : status === 'Failed'
            ? 'bg-rose-600'
            : 'bg-current'
        }`}
      />
      {status}
    </span>
  );
};
