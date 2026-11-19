import React from 'react';
import { NavLink } from 'react-router-dom';
import {
  LayoutDashboard,
  Layers,
  Activity,
  History,
  Calendar,
  Settings,
  Sparkles
} from 'lucide-react';

export const Sidebar: React.FC = () => {
  const navItems = [
    { to: '/', label: 'Dashboard', icon: LayoutDashboard },
    { to: '/jobs', label: 'Job Management', icon: Layers },
    { to: '/monitor', label: 'Live Monitor', icon: Activity },
    { to: '/history', label: 'Execution History', icon: History },
    { to: '/schedules', label: 'Schedules', icon: Calendar },
    { to: '/settings', label: 'Settings & AI', icon: Settings },
  ];

  return (
    <aside className="w-64 bg-slate-900 text-slate-300 flex flex-col shrink-0 min-h-[calc(100vh-4rem)] border-r border-slate-800">
      <div className="p-4 flex-1">
        <p className="px-3 text-[11px] font-semibold tracking-wider uppercase text-slate-500 mb-3">
          Platform
        </p>
        <nav className="space-y-1">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all ${
                    isActive
                      ? 'bg-brand-600 text-white shadow-xs shadow-brand-500/30'
                      : 'text-slate-400 hover:text-white hover:bg-slate-800/60'
                  }`
                }
              >
                <Icon className="w-4 h-4" />
                {item.label}
              </NavLink>
            );
          })}
        </nav>
      </div>

      <div className="p-4 border-t border-slate-800">
        <div className="p-3 bg-slate-800/50 rounded-xl border border-slate-700/60">
          <div className="flex items-center gap-2 text-brand-400 font-semibold text-xs mb-1">
            <Sparkles className="w-4 h-4" />
            <span>AI Failure Engine</span>
          </div>
          <p className="text-[11px] text-slate-400 leading-relaxed">
            Automated root-cause diagnostics & corrective remedies active.
          </p>
        </div>
      </div>
    </aside>
  );
};
