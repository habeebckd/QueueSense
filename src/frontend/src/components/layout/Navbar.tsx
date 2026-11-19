import React, { useState, useEffect } from 'react';
import { Bell, User as UserIcon, LogOut, Radio, RefreshCw, AlertCircle } from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { useSignalR } from '../../context/SignalRContext';
import { api } from '../../services/api';
import { NotificationItem } from '../../types';
import { NotificationDrawer } from './NotificationDrawer';

export const Navbar: React.FC = () => {
  const { user, logout } = useAuth();
  const { status } = useSignalR();
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  useEffect(() => {
    async function loadNotifications() {
      try {
        const data = await api.notifications.list(false, 10);
        setNotifications(data);
      } catch {
        // quiet error on unauthenticated
      }
    }
    loadNotifications();
    const interval = setInterval(loadNotifications, 15000);
    return () => clearInterval(interval);
  }, []);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  const getConnectionBadge = () => {
    switch (status) {
      case 'connected':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
            <Radio className="w-3.5 h-3.5 text-emerald-600 animate-pulse" />
            Live
          </span>
        );
      case 'reconnecting':
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200">
            <RefreshCw className="w-3.5 h-3.5 text-amber-600 animate-spin" />
            Reconnecting
          </span>
        );
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200">
            <AlertCircle className="w-3.5 h-3.5 text-slate-400" />
            Disconnected
          </span>
        );
    }
  };

  return (
    <>
      <header className="h-16 bg-white border-b border-slate-200 px-6 flex items-center justify-between sticky top-0 z-30 shadow-xs">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-linear-to-tr from-brand-500 to-teal-400 flex items-center justify-center text-white font-bold text-lg shadow-sm">
              Q
            </div>
            <span className="font-bold text-xl tracking-tight text-slate-900">
              Queue<span className="text-brand-500 font-extrabold">Sense</span>
            </span>
          </div>
          <div className="hidden sm:block">{getConnectionBadge()}</div>
        </div>

        <div className="flex items-center gap-4">
          <button
            onClick={() => setIsDrawerOpen(true)}
            className="relative p-2 text-slate-500 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-colors"
            title="Notifications"
          >
            <Bell className="w-5 h-5" />
            {unreadCount > 0 && (
              <span className="absolute top-1 right-1 w-4 h-4 rounded-full bg-rose-500 text-white text-[10px] font-bold flex items-center justify-center">
                {unreadCount > 9 ? '9+' : unreadCount}
              </span>
            )}
          </button>

          <div className="h-6 w-px bg-slate-200" />

          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-brand-50 border border-brand-200 flex items-center justify-center text-brand-700 font-semibold text-sm">
              {user?.name ? user.name.charAt(0).toUpperCase() : <UserIcon className="w-4 h-4" />}
            </div>
            <div className="hidden md:block text-left text-xs">
              <p className="font-semibold text-slate-800 leading-tight">{user?.name || 'Administrator'}</p>
              <p className="text-slate-500 capitalize">{user?.role || 'Operator'}</p>
            </div>
          </div>

          <button
            onClick={logout}
            className="p-2 text-slate-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors"
            title="Logout"
          >
            <LogOut className="w-5 h-5" />
          </button>
        </div>
      </header>

      <NotificationDrawer
        isOpen={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        notifications={notifications}
        onRefresh={() => {
          api.notifications.list(false, 15).then(setNotifications);
        }}
      />
    </>
  );
};
