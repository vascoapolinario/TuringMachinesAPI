
import React, { useContext, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { UserContext } from '../App';
import { api } from '../services/api';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { 
  User, Shield, Trash2, LogOut, Cpu, Layers, Star, 
  Download, ExternalLink, Box
} from 'lucide-react';
import { WorkshopItem } from '../types';

export const Profile: React.FC = () => {
  const { user, refreshUser } = useContext(UserContext);
  const navigate = useNavigate();
  const [items, setItems] = useState<WorkshopItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);

  useEffect(() => {
    const fetchStats = async () => {
      if (!user) return;
      try {
        const allItems = await api.workshop.getAll();
        const myItems = allItems.filter(i => i.author === user.username);
        setItems(myItems);
      } catch (e) {
        console.error("Failed to fetch user stats");
      } finally {
        setLoading(false);
      }
    };
    fetchStats();
  }, [user]);

  const handleLogout = () => {
    localStorage.removeItem('turing_admin_token');
    refreshUser(); 
    navigate('/login');
  };

  const handleDeleteAccount = async () => {
    if (!user) return;
    try {
      await api.players.delete(user.id);
      handleLogout();
    } catch (e) {
      console.error("Failed to delete account");
      alert("Failed to delete account. Please try again.");
    }
  };

  if (!user) return null;

  // Stats
  const machineCount = items.filter(i => i.type === 'Machine').length;
  const levelCount = items.filter(i => i.type === 'Level').length;
  const totalSubscribers = items.reduce((acc, curr) => acc + curr.subscribers, 0);
  
  // Rating calc (excluding 0s)
  const ratedItems = items.filter(i => i.rating > 0);
  const avgRating = ratedItems.length > 0 
    ? (ratedItems.reduce((acc, curr) => acc + curr.rating, 0) / ratedItems.length).toFixed(1) 
    : 'N/A';

  return (
    <div className="h-full overflow-y-auto custom-scrollbar">
      {/* --- Header Banner --- */}
      <div className="h-48 bg-slate-900 border-b border-slate-800 relative overflow-hidden group">
        <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-20"></div>
        <div className="absolute inset-0 bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 opacity-50"></div>
        
        {/* Decorative Grid */}
        <div className="absolute inset-0 opacity-10" style={{ backgroundImage: 'radial-gradient(circle, #fff 1px, transparent 1px)', backgroundSize: '24px 24px' }}></div>
      </div>

      <div className="max-w-7xl mx-auto px-6 lg:px-8 pb-12">
        {/* --- Profile Info Bar --- */}
        <div className="relative -mt-16 mb-8 flex flex-col md:flex-row items-end md:items-center gap-6">
            <div className="relative group cursor-pointer">
                <div className="w-32 h-32 rounded-2xl bg-slate-950 border-4 border-slate-950 shadow-2xl flex items-center justify-center text-slate-400 overflow-hidden relative">
                    <div className="absolute inset-0 bg-gradient-to-br from-slate-800 to-slate-900"></div>
                    <User size={64} className="relative z-10" />
                </div>
                <div className={`absolute -bottom-2 -right-2 w-8 h-8 rounded-lg flex items-center justify-center border-2 border-slate-950 shadow-lg ${
                    user.role === 'Admin' ? 'bg-purple-600 text-white' : 'bg-brand-600 text-white'
                }`}>
                    <Shield size={14} fill="currentColor" />
                </div>
            </div>

            <div className="flex-1 pb-2">
                <h1 className="text-3xl font-bold text-white tracking-tight">{user.username}</h1>
                <div className="flex items-center gap-4 text-sm text-slate-400 mt-1">
                    <span className="flex items-center gap-1.5 bg-slate-900/50 px-2 py-0.5 rounded border border-slate-800">
                        {user.role}
                    </span>
                </div>
            </div>

            <div className="flex gap-3 pb-2 w-full md:w-auto">
                <button 
                    onClick={handleLogout}
                    className="flex-1 md:flex-none px-4 py-2 bg-slate-900 hover:bg-slate-800 text-slate-300 border border-slate-800 rounded-lg text-sm font-bold transition-colors flex items-center justify-center gap-2"
                >
                    <LogOut size={16} /> Sign Out
                </button>
            </div>
        </div>

        {/* --- Stats Grid --- */}
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-10">
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 flex flex-col">
                <div className="text-slate-500 text-xs font-bold uppercase tracking-wider mb-2 flex items-center gap-2">
                    <Cpu size={14} /> Machines
                </div>
                <div className="text-2xl font-bold text-white font-mono">{machineCount}</div>
            </div>
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 flex flex-col">
                <div className="text-slate-500 text-xs font-bold uppercase tracking-wider mb-2 flex items-center gap-2">
                    <Layers size={14} /> Levels
                </div>
                <div className="text-2xl font-bold text-white font-mono">{levelCount}</div>
            </div>
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 flex flex-col">
                <div className="text-slate-500 text-xs font-bold uppercase tracking-wider mb-2 flex items-center gap-2">
                    <Download size={14} /> Subscribers
                </div>
                <div className="text-2xl font-bold text-emerald-400 font-mono">{totalSubscribers}</div>
            </div>
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 flex flex-col">
                <div className="text-slate-500 text-xs font-bold uppercase tracking-wider mb-2 flex items-center gap-2">
                    <Star size={14} /> Avg Rating
                </div>
                <div className="text-2xl font-bold text-amber-400 font-mono">{avgRating}</div>
            </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            
            {/* --- Main Content: Contributions --- */}
            <div className="lg:col-span-2 space-y-6">
                <div className="flex items-center justify-between">
                    <h2 className="text-xl font-bold text-slate-200">Recent Contributions</h2>
                    <button 
                        onClick={() => navigate(`/workshop?author=${user.username}`)}
                        className="text-sm text-brand-400 hover:text-brand-300 font-medium flex items-center gap-1"
                    >
                        View All <ExternalLink size={14} />
                    </button>
                </div>

                {loading ? (
                    <div className="flex justify-center py-10">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand-500"></div>
                    </div>
                ) : items.length === 0 ? (
                    <div className="bg-slate-900/50 border border-dashed border-slate-800 rounded-2xl p-8 text-center text-slate-500">
                        <Box size={40} className="mx-auto mb-3 opacity-50" />
                        <p>No contributions yet.</p>
                        <p className="text-sm mt-1">Go to the Simulator to start creating.</p>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {items.slice(0, 6).map(item => (
                            <div key={item.id} className="bg-slate-900 border border-slate-800 rounded-xl p-4 hover:border-slate-700 transition-all group flex gap-4">
                                <div className={`w-12 h-12 rounded-lg flex items-center justify-center shrink-0 ${
                                    item.type === 'Machine' ? 'bg-cyan-500/10 text-cyan-400' : 'bg-fuchsia-500/10 text-fuchsia-400'
                                }`}>
                                    {item.type === 'Machine' ? <Cpu size={20} /> : <Layers size={20} />}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <div className="flex justify-between items-start">
                                        <h3 className="font-bold text-slate-200 truncate group-hover:text-white transition-colors">{item.name}</h3>
                                        <div className="flex items-center gap-1 text-amber-400 text-xs font-bold">
                                            <Star size={10} fill="currentColor" /> {item.rating.toFixed(1)}
                                        </div>
                                    </div>
                                    <p className="text-xs text-slate-500 truncate mt-0.5">{item.description}</p>
                                    <div className="flex items-center gap-4 mt-3 text-xs text-slate-500">
                                        <span className="flex items-center gap-1">
                                            <Download size={12} /> {item.subscribers}
                                        </span>
                                        <span className="px-1.5 py-0.5 bg-slate-950 rounded border border-slate-800">
                                            {item.type}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </div>

            {/* --- Sidebar: Account Settings --- */}
            <div className="space-y-6">
                <div>
                    <h2 className="text-xl font-bold text-slate-200 mb-6">Account Settings</h2>
                    
                    <div className="bg-slate-900 border border-slate-800 rounded-xl overflow-hidden">
                        <div className="p-4 border-b border-slate-800">
                            <h3 className="text-sm font-bold text-slate-300">Identity</h3>
                            <div className="mt-3 space-y-2 text-sm">
                                <div className="flex justify-between py-2 border-b border-slate-800/50">
                                    <span className="text-slate-500">Username</span>
                                    <span className="text-slate-200 font-mono">{user.username}</span>
                                </div>
                                <div className="flex justify-between py-2">
                                    <span className="text-slate-500">User ID</span>
                                    <span className="text-slate-200 font-mono">#{user.id}</span>
                                </div>
                            </div>
                        </div>
                        
                        <div className="p-4 bg-red-500/5">
                            <h3 className="text-sm font-bold text-red-400 flex items-center gap-2 mb-2">
                                <Trash2 size={16} /> Danger Zone
                            </h3>
                            <p className="text-xs text-slate-500 mb-4">
                                Permanently delete your account and all associated workshop items.
                            </p>
                            <button 
                                onClick={() => setConfirmDeleteOpen(true)}
                                className="w-full py-2 bg-red-500/10 hover:bg-red-500/20 text-red-400 border border-red-500/20 rounded-lg text-xs font-bold transition-colors"
                            >
                                Delete Account
                            </button>
                        </div>
                    </div>
                </div>
            </div>

        </div>

        <ConfirmDialog 
            isOpen={confirmDeleteOpen}
            title="Delete Account?"
            message="Are you sure you want to delete your account? This will permanently remove your profile, all submitted machines/levels, and leaderboard entries. This action cannot be undone."
            confirmText="Delete Everything"
            isDestructive={true}
            onCancel={() => setConfirmDeleteOpen(false)}
            onConfirm={handleDeleteAccount}
        />
      </div>
    </div>
  );
};
