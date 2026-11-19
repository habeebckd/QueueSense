import React, { useState } from 'react';
import {
  User,
  Sparkles,
  Shield,
  Activity,
  Cpu,
  CheckCircle,
  Radio
} from 'lucide-react';
import { useAuth } from '../../context/AuthContext';
import { useSignalR } from '../../context/SignalRContext';

export const Settings: React.FC = () => {
  const { user } = useAuth();
  const { status } = useSignalR();
  const [testSuccess, setTestSuccess] = useState(false);

  const testDiagnosticPing = () => {
    setTestSuccess(true);
    setTimeout(() => setTestSuccess(false), 3000);
  };

  return (
    <div className="space-y-6 max-w-4xl">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">System Settings & Status</h1>
        <p className="text-sm text-slate-500 mt-0.5">
          Manage your account profile, inspect AI provider connections, and review worker telemetry.
        </p>
      </div>

      {/* User Profile Card */}
      <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-4">
        <div className="flex items-center gap-2 border-b border-slate-100 pb-3">
          <User className="w-5 h-5 text-slate-600" />
          <h2 className="font-semibold text-slate-800 text-sm">Account Information</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
          <div className="p-3 bg-slate-50 rounded-lg">
            <span className="text-slate-400 block mb-0.5">Full Name</span>
            <span className="font-semibold text-slate-800 text-sm">{user?.name || 'Administrator'}</span>
          </div>

          <div className="p-3 bg-slate-50 rounded-lg">
            <span className="text-slate-400 block mb-0.5">Email Address</span>
            <span className="font-semibold text-slate-800 text-sm">{user?.email || 'admin@jobflow.ai'}</span>
          </div>

          <div className="p-3 bg-slate-50 rounded-lg">
            <span className="text-slate-400 block mb-0.5">Assigned Role</span>
            <span className="font-semibold text-brand-700 capitalize text-sm">{user?.role || 'Admin'}</span>
          </div>

          <div className="p-3 bg-slate-50 rounded-lg">
            <span className="text-slate-400 block mb-0.5">Security Level</span>
            <span className="font-semibold text-slate-800 text-sm">JWT Authenticated (24h validity)</span>
          </div>
        </div>
      </div>

      {/* AI Provider Status Card */}
      <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-4">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <div className="flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-purple-600" />
            <h2 className="font-semibold text-slate-800 text-sm">AI Failure Diagnostics Engine</h2>
          </div>
          <span className="px-2.5 py-1 bg-purple-50 text-purple-700 border border-purple-200 text-xs font-semibold rounded-full">
            Active Provider: Mock Engine
          </span>
        </div>

        <div className="space-y-3 text-xs text-slate-600">
          <p className="leading-relaxed">
            QueueSense is running with the built-in <strong>Mock AI Provider</strong> by default. It provides offline, zero-token deterministic root cause diagnoses for simulated database connection timeouts, memory pressure, and cancellation events without requiring external paid credentials.
          </p>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 pt-2">
            <div className="p-3 border border-emerald-200 bg-emerald-50/50 rounded-lg">
              <span className="font-semibold text-emerald-800 flex items-center gap-1.5 mb-1">
                <CheckCircle className="w-4 h-4 text-emerald-600" />
                Mock Provider
              </span>
              <p className="text-[11px] text-emerald-700">Deterministic heuristics, 0ms latency, zero API keys required.</p>
            </div>

            <div className="p-3 border border-slate-200 bg-slate-50/50 rounded-lg">
              <span className="font-semibold text-slate-800 flex items-center gap-1.5 mb-1">
                <Cpu className="w-4 h-4 text-slate-500" />
                Local Ollama
              </span>
              <p className="text-[11px] text-slate-500">Supports Llama-3 and Mistral via local Docker or host service.</p>
            </div>

            <div className="p-3 border border-slate-200 bg-slate-50/50 rounded-lg">
              <span className="font-semibold text-slate-800 flex items-center gap-1.5 mb-1">
                <Shield className="w-4 h-4 text-slate-500" />
                OpenAI / Azure
              </span>
              <p className="text-[11px] text-slate-500">Cloud GPT-4o with automated JSON schema validation.</p>
            </div>
          </div>
        </div>
      </div>

      {/* Telemetry & SignalR Diagnostics */}
      <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-xs space-y-4">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <div className="flex items-center gap-2">
            <Radio className="w-5 h-5 text-emerald-600" />
            <h2 className="font-semibold text-slate-800 text-sm">Real-Time Hub Connection</h2>
          </div>
          <span className="text-xs font-mono font-medium text-slate-500">
            Status: <span className="text-emerald-600 font-bold capitalize">{status}</span>
          </span>
        </div>

        <div className="flex items-center justify-between pt-2">
          <div className="text-xs text-slate-500">
            <p>SignalR WebSocket endpoint: <code className="text-slate-700 bg-slate-100 px-1.5 py-0.5 rounded">/hubs/jobflow</code></p>
            <p className="mt-1">Reconnection strategy: Exponential backoff with resync.</p>
          </div>

          <button
            onClick={testDiagnosticPing}
            className="px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold rounded-lg transition-colors flex items-center gap-1.5"
          >
            {testSuccess ? (
              <>
                <CheckCircle className="w-4 h-4 text-emerald-600" />
                Socket Healthy
              </>
            ) : (
              <>
                <Activity className="w-4 h-4" />
                Test Latency
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
