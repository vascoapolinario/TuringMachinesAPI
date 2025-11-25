
import React, { useEffect, useState, useContext } from 'react';
import { api } from '../services/api';
import { Player } from '../types';
import { Card } from '../components/Card';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { 
  Search, Trash2, User, ShieldAlert, Lock, Calendar, Clock, 
  Download, Users, Shield, UserPlus, FileDown
} from 'lucide-react';
import { UserContext } from '../App';

export const Players: React.FC = () => {
  const { user } = useContext(UserContext);
  const [players, setPlayers] = useState<Player[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState('');
  
  // Dialog State
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [playerToDelete, setPlayerToDelete] = useState<number | null>(null);

  const fetchPlayers = async () => {
    try {
      setLoading(true);
      const data = await api.players.getAll();
      setPlayers(data);
    } catch (e: any) {
      if (e.message?.includes('403') || e.message?.includes('Forbidden')) {
         setError('Access Denied');
      } else {
         setError('Failed to load players');
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user?.role === 'Admin') {
      fetchPlayers();
    } else {
      setLoading(false);
    }
  }, [user]);

  const confirmDelete = (id: number) => {
    setPlayerToDelete(id);
    setConfirmOpen(true);
  };

  const handleExecuteDelete = async () => {
    if (!playerToDelete) return;
    try {
      await api.players.delete(playerToDelete);
      setPlayers(prev => prev.filter(p => p.id !== playerToDelete));
    } catch (e) {
      console.error('Failed to delete player.');
    } finally {
      setConfirmOpen(false);
      setPlayerToDelete(null);
    }
  };

  const handleExportCSV = () => {
    const headers = ['ID', 'Username', 'Role', 'Joined', 'Last Active'];
    const csvContent = [
        headers.join(','),
        ...filteredPlayers.map(p => [
            p.id, 
            p.username, 
            p.role, 
            p.createdAt, 
            p.lastLogin || 'Never'
        ].join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `turing_players_${new Date().toISOString().split('T')[0]}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const filteredPlayers = players
    .filter(p => 
      p.username.toLowerCase().includes(searchTerm.toLowerCase()) || 
      p.id.toString().includes(searchTerm)
    )
    .sort((a, b) => a.id - b.id);

  const formatDate = (dateString: string | null) => {
    if (!dateString || dateString.startsWith('0001')) return <span className="text-slate-600 italic">N/A</span>;
    return new Date(dateString).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
  };

  const getLastActiveStatus = (dateString: string | null) => {
     if (!dateString) return 'offline';
     const diff = new Date().getTime() - new Date(dateString).getTime();
     const minutes = diff / 60000;
     if (minutes < 15) return 'online'; // Active now
     if (minutes < 1440) return 'recent'; // Active today
     return 'offline';
  };

  const formatLastActive = (dateString: string | null) => {
      if (!dateString) return "Never";
      const d = new Date(dateString);
      const now = new Date();
      const diffMs = now.getTime() - d.getTime();
      const diffMins = Math.floor(diffMs / 60000);
      const diffHours = Math.floor(diffMins / 60);
      
      if (diffMins < 1) return "Just now";
      if (diffMins < 60) return `${diffMins}m ago`;
      if (diffHours < 24) return `${diffHours}h ago`;
      return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  };

  // Stats calculation
  const stats = {
      total: players.length,
      admins: players.filter(p => p.role === 'Admin').length,
      newToday: players.filter(p => {
          const d = new Date(p.createdAt);
          const now = new Date();
          return d.getDate() === now.getDate() && d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
      }).length
  };

  // Handle Non-Admin View
  if (user?.role !== 'Admin') {
    return (
      <div className="h-full flex flex-col items-center justify-center text-slate-500 animate-in fade-in duration-500">
         <div className="p-8 bg-slate-900/50 rounded-full mb-6 border border-slate-800 shadow-2xl relative">
            <div className="absolute inset-0 bg-red-500/10 rounded-full animate-pulse"></div>
            <Lock size={64} className="text-slate-600 relative z-10" />
         </div>
         <h2 className="text-2xl font-bold text-slate-200 tracking-tight">Restricted Access</h2>
         <p className="text-sm mt-3 max-w-sm text-center text-slate-500 leading-relaxed font-mono">
           Security Clearance Level 5 Required.<br/>
           Access to the personnel registry is limited to administrators.
         </p>
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto custom-scrollbar p-6 lg:p-8">
    <div className="max-w-7xl mx-auto space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
      
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between md:items-end gap-6">
        <div>
          <h1 className="text-3xl font-bold text-slate-100 flex items-center gap-3">
            <Users className="text-brand-400" size={32} /> Personnel Registry
          </h1>
          <p className="text-slate-400 text-sm mt-2 max-w-lg">
            Manage user accounts, roles, and security clearance.
          </p>
        </div>
        <div className="flex gap-4">
             <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 min-w-[120px]">
                 <div className="text-[10px] uppercase font-bold text-slate-500 mb-1 flex items-center gap-2">
                    <Users size={12} /> Total Users
                 </div>
                 <div className="text-2xl font-bold text-white font-mono">{loading ? '-' : stats.total}</div>
             </div>
             <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 min-w-[120px]">
                 <div className="text-[10px] uppercase font-bold text-slate-500 mb-1 flex items-center gap-2">
                    <Shield size={12} /> Admins
                 </div>
                 <div className="text-2xl font-bold text-purple-400 font-mono">{loading ? '-' : stats.admins}</div>
             </div>
             <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 min-w-[120px]">
                 <div className="text-[10px] uppercase font-bold text-slate-500 mb-1 flex items-center gap-2">
                    <UserPlus size={12} /> New (24h)
                 </div>
                 <div className="text-2xl font-bold text-emerald-400 font-mono">{loading ? '-' : stats.newToday}</div>
             </div>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
          <div className="relative w-full md:w-96 group">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors" size={16} />
            <input 
                type="text"
                placeholder="Search by username or ID..."
                className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-4 text-slate-200 text-sm focus:outline-none focus:border-brand-500/50 focus:ring-1 focus:ring-brand-500/50 transition-all shadow-inner"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <button 
                onClick={handleExportCSV}
                className="flex items-center gap-2 px-4 py-3 bg-slate-900 hover:bg-slate-800 text-slate-300 rounded-xl transition-all border border-slate-800 hover:border-slate-700 shadow-sm font-medium text-sm group"
          >
                <FileDown size={16} className="text-slate-500 group-hover:text-brand-400 transition-colors" />
                Export CSV
          </button>
      </div>

      {/* Table Card */}
      <div className="bg-slate-900 border border-slate-800 rounded-2xl shadow-xl overflow-hidden flex flex-col min-h-[500px]">
        {loading ? (
          <div className="flex-1 flex flex-col items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand-500"></div>
              <span className="text-slate-500 font-mono text-sm">Accessing database...</span>
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
                  <th className="px-6 py-5 w-20">ID</th>
                  <th className="px-6 py-5">Identity</th>
                  <th className="px-6 py-5">Clearance</th>
                  <th className="px-6 py-5">Joined</th>
                  <th className="px-6 py-5">Last Activity</th>
                  <th className="px-6 py-5 text-right">Manage</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/50">
                {filteredPlayers.map((player) => {
                    const status = getLastActiveStatus(player.lastLogin);
                    const isAdmin = player.role === 'Admin';
                    
                    return (
                  <tr key={player.id} className="group hover:bg-slate-800/30 transition-colors">
                    <td className="px-6 py-4">
                        <span className="font-mono text-xs text-slate-600 group-hover:text-slate-400">#{player.id}</span>
                    </td>
                    <td className="px-6 py-4 text-slate-200">
                      <div className="flex items-center gap-3">
                        <div className={`w-10 h-10 rounded-lg flex items-center justify-center text-sm font-bold shadow-lg ${
                            isAdmin 
                            ? 'bg-gradient-to-br from-purple-900 to-slate-900 text-purple-200 border border-purple-500/30' 
                            : 'bg-gradient-to-br from-slate-800 to-slate-900 text-slate-300 border border-slate-700'
                        }`}>
                          {player.username.substring(0, 2).toUpperCase()}
                        </div>
                        <div>
                            <div className="font-bold text-sm group-hover:text-white transition-colors">{player.username}</div>
                            {user?.username === player.username && (
                                <span className="text-[10px] text-brand-400 font-medium">It's you</span>
                            )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                       <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold uppercase tracking-wider border ${
                         isAdmin
                           ? 'bg-purple-500/10 text-purple-400 border-purple-500/20 shadow-[0_0_10px_rgba(168,85,247,0.1)]' 
                           : 'bg-slate-900 text-slate-500 border-slate-700'
                       }`}>
                          {isAdmin && <Shield size={10} fill="currentColor" />}
                          {player.role}
                       </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-400">
                        <div className="flex items-center gap-2">
                            <Calendar size={14} className="text-slate-600" />
                            {formatDate(player.createdAt)}
                        </div>
                    </td>
                    <td className="px-6 py-4 text-sm text-slate-400">
                        <div className="flex items-center gap-2.5">
                            <div className="relative flex h-2.5 w-2.5">
                                {status === 'online' && <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>}
                                <span className={`relative inline-flex rounded-full h-2.5 w-2.5 ${
                                    status === 'online' ? 'bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.4)]' : 
                                    status === 'recent' ? 'bg-amber-500' : 'bg-slate-700'
                                }`}></span>
                            </div>
                            <span className={`font-mono text-xs ${status === 'online' ? 'text-emerald-400 font-bold' : 'text-slate-500'}`}>
                                {status === 'online' ? 'Online Now' : formatLastActive(player.lastLogin)}
                            </span>
                        </div>
                    </td>
                    <td className="px-6 py-4 text-right">
                      {user?.role === 'Admin' && player.id !== user.id && (
                          <button 
                            onClick={() => confirmDelete(player.id)}
                            className="p-2 text-slate-600 hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-all"
                            title="Delete Account"
                          >
                            <Trash2 size={16} />
                          </button>
                      )}
                    </td>
                  </tr>
                )})}
                {filteredPlayers.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-6 py-20 text-center text-slate-500">
                      <div className="flex flex-col items-center justify-center">
                          <Search size={32} className="mb-2 opacity-50" />
                          <p>No personnel records match "{searchTerm}"</p>
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
        title="Revoke Access?"
        message={`Are you sure you want to delete player #${playerToDelete}? This will permanently remove their account and all associated data.`}
        confirmText="Revoke & Delete"
        isDestructive={true}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={handleExecuteDelete}
      />
    </div>
    </div>
  );
};
