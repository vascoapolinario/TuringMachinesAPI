
import React, { useEffect, useState, useContext } from 'react';
import { api } from '../services/api';
import { AdminLog } from '../types';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { 
  Search, Trash2, ScrollText, CalendarClock, ShieldAlert, User, Eraser, 
  Activity, ArrowRight, PlusCircle, Edit3, AlertTriangle, ShieldCheck, RefreshCw
} from 'lucide-react';
import { UserContext } from '../App';

export const AdminLogs: React.FC = () => {
  const { user } = useContext(UserContext);
  const [logs, setLogs] = useState<AdminLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState('');

  // Dialog States
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [logToDelete, setLogToDelete] = useState<number | null>(null);
  
  const [cleanupOpen, setCleanupOpen] = useState(false);
  const [cleanupMode, setCleanupMode] = useState<'all' | 'old'>('old');

  const fetchLogs = async (force = false) => {
    try {
      if (force) setRefreshing(true);
      else setLoading(true);
      
      const data = await api.logs.getAll(force);
      setLogs(data);
    } catch (e: any) {
      if (e.message?.includes('403') || e.message?.includes('Forbidden')) {
         setError('Access Denied');
      } else {
         setError('Failed to load logs');
      }
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    // Only fetch if admin to save resources, though API protects it too
    if (user?.role === 'Admin') {
      fetchLogs();
    } else {
      setLoading(false);
    }
  }, [user]);

  // Single Delete
  const confirmDelete = (id: number) => {
    setLogToDelete(id);
    setConfirmOpen(true);
  };

  const handleExecuteDelete = async () => {
    if (!logToDelete) return;
    try {
      await api.logs.delete(logToDelete);
      setLogs(prev => prev.filter(l => l.id !== logToDelete));
    } catch (e) {
      console.error('Failed to delete log.');
    } finally {
      setConfirmOpen(false);
      setLogToDelete(null);
    }
  };

  // Bulk Cleanup
  const handleExecuteCleanup = async () => {
      try {
          const timeSpan = cleanupMode === 'old' ? '30.00:00:00' : undefined; 
          await api.logs.bulkDelete(timeSpan);
          fetchLogs(true); // Refresh list
      } catch (e) {
          console.error('Cleanup failed');
          alert('Failed to clean up logs');
      } finally {
          setCleanupOpen(false);
      }
  };

  const filteredLogs = logs.filter(l => 
    l.actorName.toLowerCase().includes(searchTerm.toLowerCase()) || 
    (l.targetEntityName && l.targetEntityName.toLowerCase().includes(searchTerm.toLowerCase())) ||
    l.action.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const formatDateTime = (dateString: string) => {
     try {
         return new Date(dateString).toLocaleString(undefined, {
             month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit', second: '2-digit'
         });
     } catch { return dateString; }
  };

  const getActionStyle = (action: string) => {
      const a = action.toLowerCase();
      if (a.includes('delete') || a.includes('kick') || a.includes('ban') || a.includes('remove')) {
          return {
              bg: 'bg-red-500/10',
              text: 'text-red-400',
              border: 'border-red-500/20',
              icon: <Trash2 size={12} />
          };
      }
      if (a.includes('create') || a.includes('register') || a.includes('add')) {
          return {
              bg: 'bg-emerald-500/10',
              text: 'text-emerald-400',
              border: 'border-emerald-500/20',
              icon: <PlusCircle size={12} />
          };
      }
      if (a.includes('update') || a.includes('modify') || a.includes('edit')) {
          return {
              bg: 'bg-amber-500/10',
              text: 'text-amber-400',
              border: 'border-amber-500/20',
              icon: <Edit3 size={12} />
          };
      }
      return {
          bg: 'bg-slate-800',
          text: 'text-slate-400',
          border: 'border-slate-700',
          icon: <Activity size={12} />
      };
  };

  if (user?.role !== 'Admin') {
    return (
      <div className="h-full flex flex-col items-center justify-center text-slate-500 animate-in fade-in duration-500">
         <div className="p-8 bg-slate-900/50 rounded-full mb-6 border border-slate-800 shadow-2xl relative">
            <div className="absolute inset-0 bg-red-500/10 rounded-full animate-pulse"></div>
            <ShieldAlert size={64} className="text-slate-600 relative z-10" />
         </div>
         <h2 className="text-2xl font-bold text-slate-200 tracking-tight">Restricted Access</h2>
         <p className="text-sm mt-3 max-w-sm text-center text-slate-500 leading-relaxed font-mono">
           Security Clearance Level 5 Required.<br/>
           Audit logs are classified information available only to administrators.
         </p>
      </div>
    );
  }

  // Stats
  const stats = {
      total: logs.length,
      today: logs.filter(l => {
          const d = new Date(l.doneAt);
          const now = new Date();
          return d.getDate() === now.getDate() && d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
      }).length
  };

  return (
    <div className="h-full overflow-y-auto custom-scrollbar p-6 lg:p-8">
    <div className="max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
      
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between md:items-end gap-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-100 flex items-center gap-3">
            <ScrollText className="text-brand-400" size={32} /> System Audit Logs
          </h1>
          <p className="text-slate-400 text-sm mt-2 max-w-lg">
            Track privileged operations, security events, and administrative actions across the platform.
          </p>
        </div>
        
        <div className="flex gap-4">
             <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 min-w-[120px]">
                 <div className="text-[10px] uppercase font-bold text-slate-500 mb-1 flex items-center gap-2">
                    <ShieldCheck size={12} /> Total Records
                 </div>
                 <div className="text-2xl font-bold text-white font-mono">{loading ? '-' : stats.total}</div>
             </div>
             <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 min-w-[120px]">
                 <div className="text-[10px] uppercase font-bold text-slate-500 mb-1 flex items-center gap-2">
                    <Activity size={12} /> Events (24h)
                 </div>
                 <div className="text-2xl font-bold text-brand-400 font-mono">{loading ? '-' : stats.today}</div>
             </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
          <div className="relative w-full md:w-96 group">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors" size={16} />
            <input 
                type="text"
                placeholder="Search by actor, action or target..."
                className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-4 text-slate-200 text-sm focus:outline-none focus:border-brand-500/50 focus:ring-1 focus:ring-brand-500/50 transition-all shadow-inner"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          
          <div className="flex gap-3">
            <button 
                  onClick={() => fetchLogs(true)}
                  className="p-3 bg-slate-900 hover:bg-slate-800 text-slate-300 rounded-xl transition-all border border-slate-800 hover:border-slate-700 shadow-sm"
                  title="Refresh List"
            >
                  <RefreshCw size={16} className={refreshing ? 'animate-spin text-brand-500' : ''} />
            </button>
            <button 
                onClick={() => { setCleanupMode('old'); setCleanupOpen(true); }}
                className="px-4 py-3 bg-slate-900 hover:bg-slate-800 text-slate-300 rounded-xl text-xs font-bold transition-colors border border-slate-800 hover:border-slate-700 shadow-sm flex items-center gap-2"
            >
                <Eraser size={14} /> Cleanup Old (30d+)
            </button>
            <button 
                onClick={() => { setCleanupMode('all'); setCleanupOpen(true); }}
                className="px-4 py-3 bg-red-500/10 hover:bg-red-500/20 text-red-400 rounded-xl text-xs font-bold transition-colors border border-red-500/20 hover:border-red-500/30 flex items-center gap-2"
            >
                <Trash2 size={14} /> Clear All
            </button>
          </div>
      </div>

      {/* Table Card */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl shadow-xl overflow-hidden flex flex-col min-h-[500px]">
        {loading ? (
          <div className="flex-1 flex flex-col items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand-500"></div>
              <span className="text-slate-500 font-mono text-sm">Retrieving audit trail...</span>
          </div>
        ) : error ? (
          <div className="flex-1 flex flex-col items-center justify-center text-red-400 gap-2">
             <ShieldAlert size={32} />
             {error}
          </div>
        ) : (
          <div className="overflow-x-auto custom-scrollbar flex-1">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-800 bg-slate-950/50 text-slate-500 text-xs uppercase tracking-wider font-bold">
                  <th className="px-6 py-5">Timestamp</th>
                  <th className="px-6 py-5">Actor</th>
                  <th className="px-6 py-5">Operation</th>
                  <th className="px-6 py-5">Target Entity</th>
                  <th className="px-6 py-5 text-right">Manage</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/50">
                {filteredLogs.map((log) => {
                  const style = getActionStyle(log.action);
                  return (
                  <tr key={log.id} className="group hover:bg-slate-800/30 transition-colors">
                    <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2 text-slate-500">
                            <CalendarClock size={14} className="text-slate-600" />
                            <span className="font-mono text-xs">{formatDateTime(log.doneAt)}</span>
                        </div>
                    </td>
                    <td className="px-6 py-4 text-slate-200">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-lg bg-slate-950 border border-slate-800 flex items-center justify-center text-xs font-bold text-slate-400">
                             {log.actorName.charAt(0).toUpperCase()}
                        </div>
                        <div>
                             <div className="font-bold text-xs">{log.actorName}</div>
                             <div className="text-[10px] text-purple-400 font-medium">{log.actorRole}</div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                       <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold uppercase tracking-wider border ${style.bg} ${style.text} ${style.border}`}>
                          {style.icon}
                          {log.action}
                       </span>
                    </td>
                    <td className="px-6 py-4">
                        <div className="flex flex-col">
                            <span className="text-slate-200 text-sm font-medium">{log.targetEntityName || <span className="text-slate-600 italic">Unknown</span>}</span>
                            <span className="text-xs text-slate-500 flex items-center gap-1.5 mt-0.5">
                                <span className="px-1.5 py-0.5 bg-slate-950 rounded border border-slate-800 text-[10px] font-mono">{log.targetEntityType}</span>
                                <span className="font-mono opacity-50">#{log.targetEntityId}</span>
                            </span>
                        </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                        <button 
                          onClick={() => confirmDelete(log.id)}
                          className="p-2 text-slate-600 hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-all opacity-0 group-hover:opacity-100"
                          title="Delete Log Entry"
                        >
                          <Trash2 size={16} />
                        </button>
                    </td>
                  </tr>
                )})}
                {filteredLogs.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-6 py-20 text-center text-slate-500">
                      <div className="flex flex-col items-center justify-center">
                          <ScrollText size={32} className="mb-2 opacity-50" />
                          <p>No log records match your search.</p>
                      </div>
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <ConfirmDialog 
        isOpen={confirmOpen}
        title="Delete Audit Record?"
        message={`Are you sure you want to delete log #${logToDelete}? This audit record will be permanently lost.`}
        confirmText="Delete Record"
        isDestructive={true}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={handleExecuteDelete}
      />
      
      <ConfirmDialog 
        isOpen={cleanupOpen}
        title={cleanupMode === 'all' ? "Wipe Audit Trail?" : "Prune Old Logs?"}
        message={cleanupMode === 'all' 
            ? "You are about to wipe the ENTIRE system audit trail. This action is irreversible and should only be performed during system maintenance." 
            : "This will permanently delete all logs older than 30 days. Continue?"}
        confirmText={cleanupMode === 'all' ? "Wipe Everything" : "Prune Logs"}
        isDestructive={true}
        onCancel={() => setCleanupOpen(false)}
        onConfirm={handleExecuteCleanup}
      />
    </div>
    </div>
  );
};
