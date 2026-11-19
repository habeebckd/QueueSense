import React from 'react';
import { X, CheckCheck, AlertTriangle, Info, Clock, ExternalLink } from 'lucide-react';
import { NotificationItem } from '../../types';
import { api } from '../../services/api';
import { useNavigate } from 'react-router-dom';

interface NotificationDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  notifications: NotificationItem[];
  onRefresh: () => void;
}

export const NotificationDrawer: React.FC<NotificationDrawerProps> = ({
  isOpen,
  onClose,
  notifications,
  onRefresh,
}) => {
  const navigate = useNavigate();

  if (!isOpen) return null;

  const handleMarkAsRead = async (id: string) => {
    try {
      await api.notifications.markRead(id);
      onRefresh();
    } catch {
      // quiet
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await api.notifications.markAllRead();
      onRefresh();
    } catch {
      // quiet
    }
  };

  const getIcon = (type: string) => {
    if (type.includes('Failed') || type.includes('TimedOut')) {
      return <AlertTriangle className="w-4 h-4 text-rose-500" />;
    }
    if (type.includes('AiAnalysis')) {
      return <Info className="w-4 h-4 text-brand-500" />;
    }
    return <Clock className="w-4 h-4 text-blue-500" />;
  };

  return (
    <div className="fixed inset-0 z-50 overflow-hidden bg-slate-900/40 backdrop-blur-xs flex justify-end animate-in fade-in">
      <div className="w-full max-w-md bg-white shadow-2xl h-full flex flex-col border-l border-slate-200">
        <div className="p-4 border-b border-slate-200 flex items-center justify-between bg-slate-50">
          <div className="flex items-center gap-2">
            <h3 className="font-semibold text-slate-800">Notifications</h3>
            <span className="text-xs px-2 py-0.5 rounded-full bg-slate-200 text-slate-700 font-medium">
              {notifications.length}
            </span>
          </div>
          <div className="flex items-center gap-2">
            <button
              onClick={handleMarkAllRead}
              className="text-xs text-brand-600 hover:text-brand-800 font-medium flex items-center gap-1 p-1 hover:bg-brand-50 rounded"
              title="Mark all as read"
            >
              <CheckCheck className="w-3.5 h-3.5" />
              Mark all
            </button>
            <button
              onClick={onClose}
              className="p-1 rounded-lg text-slate-400 hover:text-slate-600 hover:bg-slate-200"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto divide-y divide-slate-100 p-2">
          {notifications.length === 0 ? (
            <div className="text-center py-12 text-slate-400">
              <p className="text-sm">No notifications yet</p>
            </div>
          ) : (
            notifications.map((n) => (
              <div
                key={n.id}
                className={`p-3 rounded-lg transition-colors flex gap-3 text-left ${
                  n.isRead ? 'bg-white hover:bg-slate-50' : 'bg-brand-50/40 hover:bg-brand-50/70'
                }`}
              >
                <div className="mt-0.5 shrink-0">{getIcon(n.type)}</div>
                <div className="flex-1 min-w-0">
                  <p className="text-xs font-semibold text-slate-800">{n.type}</p>
                  <p className="text-xs text-slate-600 mt-0.5 leading-relaxed break-words">{n.message}</p>
                  <div className="flex items-center justify-between mt-2 pt-1">
                    <span className="text-[10px] text-slate-400">
                      {new Date(n.createdAt).toLocaleTimeString()}
                    </span>
                    <div className="flex items-center gap-2">
                      {n.executionId && (
                        <button
                          onClick={() => {
                            onClose();
                            navigate(`/monitor?executionId=${n.executionId}`);
                          }}
                          className="text-[11px] text-brand-600 hover:text-brand-800 font-medium flex items-center gap-0.5"
                        >
                          View <ExternalLink className="w-3 h-3" />
                        </button>
                      )}
                      {!n.isRead && (
                        <button
                          onClick={() => handleMarkAsRead(n.id)}
                          className="text-[11px] text-slate-400 hover:text-slate-700 font-medium"
                        >
                          Dismiss
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};
