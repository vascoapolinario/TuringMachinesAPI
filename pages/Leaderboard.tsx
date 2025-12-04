
import React, { useEffect, useState, useContext } from 'react';
import { api } from '../services/api';
import { LeaderboardEntry } from '../types';
import { UserContext } from '../App';
import { Modal } from '../components/Modal';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { Trophy, Medal, Timer, Cpu, Network, Plus, Crown, Search, LayoutList, User, Zap, RefreshCw, Trash2 } from 'lucide-react';

export const Leaderboard: React.FC = () => {
  const { user } = useContext(UserContext);
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');

  // Filters
  const [viewMode, setViewMode] = useState<'Global' | 'Personal'>('Global');
  const [sortBy, setSortBy] = useState<'default' | 'time' | 'nodes' | 'connections'>('default');
  const [selectedLevel, setSelectedLevel] = useState<string>('');
  const [availableLevels, setAvailableLevels] = useState<string[]>([]);

  // Admin Register Modal
  const [isAdminModalOpen, setAdminModalOpen] = useState(false);
  const [newLevelName, setNewLevelName] = useState('');
  const [newLevelCategory, setNewLevelCategory] = useState('Workshop');
  const [newLevelWorkshopId, setNewLevelWorkshopId] = useState('');
  const [submittingLevel, setSubmittingLevel] = useState(false);

  // Admin Delete State
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [submissionToDelete, setSubmissionToDelete] = useState<{player: string, level: string} | null>(null);

  const fetchData = async (force = false) => {
    try {
      if (force) setRefreshing(true);
      else setLoading(true);
      
      const data = await api.leaderboard.get(
        viewMode === 'Personal',
        selectedLevel || undefined,
        force
      );
      setEntries(data);

      if (!selectedLevel) {
          const levels = Array.from(new Set(data.map(e => e.levelName)));
          setAvailableLevels(levels.sort());
      }
    } catch (e) {
      setError("Failed to load leaderboard data");
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [viewMode, selectedLevel]);

  const getSortedEntries = () => {
    const data = [...entries];
    if (sortBy === 'time') return data.sort((a, b) => a.time - b.time);
    if (sortBy === 'nodes') return data.sort((a, b) => a.nodeCount - b.nodeCount);
    if (sortBy === 'connections') return data.sort((a, b) => a.connectionCount - b.connectionCount);
    return data; 
  };

  const sortedEntries = getSortedEntries();

  const handleRegisterLevel = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmittingLevel(true);
    try {
      await api.leaderboard.registerLevel(
          newLevelName, 
          newLevelCategory, 
          newLevelWorkshopId ? parseInt(newLevelWorkshopId) : undefined
      );
      setAdminModalOpen(false);
      setNewLevelName('');
      setNewLevelWorkshopId('');
    } catch (e) {
      alert("Failed to register level. Ensure name is unique and you are Admin.");
    } finally {
      setSubmittingLevel(false);
    }
  };

  const confirmDelete = (player: string, level: string) => {
    setSubmissionToDelete({ player, level });
    setDeleteConfirmOpen(true);
  };

  const handleExecuteDelete = async () => {
    if (!submissionToDelete) return;
    try {
      await api.leaderboard.deleteSubmission(submissionToDelete.player, submissionToDelete.level);
      setEntries(prev => prev.filter(e => !(e.playerName === submissionToDelete.player && e.levelName === submissionToDelete.level)));
    } catch (e) {
      alert("Failed to delete submission.");
    } finally {
      setDeleteConfirmOpen(false);
      setSubmissionToDelete(null);
    }
  };

  const formatTime = (totalSeconds: number) => {
    if (totalSeconds < 60) {
      return `${totalSeconds.toFixed(2)}s`;
    }
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = Math.floor(totalSeconds % 60);

    if (hours > 0) {
      return `${hours}h ${minutes}m ${seconds > 0 ? `${seconds}s` : ''}`;
    }
    return `${minutes}m ${seconds}s`;
  };

  // --- Components ---

  const PodiumStep = ({ entry, rank }: { entry: LeaderboardEntry | undefined, rank: 1 | 2 | 3 }) => {
    if (!entry) return <div className="flex-1"></div>;

    const isGold = rank === 1;
    const isSilver = rank === 2;
    const isBronze = rank === 3;
    
    let colorClass = "";
    let heightClass = "";
    let icon = null;

    if (isGold) {
        colorClass = "from-amber-500/20 to-amber-900/10 border-amber-500/30 text-amber-400";
        heightClass = "h-40";
        icon = <Crown size={32} className="text-amber-400 mb-2 drop-shadow-glow" />;
    } else if (isSilver) {
        colorClass = "from-slate-400/20 to-slate-800/10 border-slate-400/30 text-slate-300";
        heightClass = "h-32";
        icon = <Medal size={28} className="text-slate-300 mb-2" />;
    } else {
        colorClass = "from-orange-700/20 to-orange-900/10 border-orange-700/30 text-orange-400";
        heightClass = "h-24";
        icon = <Medal size={24} className="text-orange-600 mb-2" />;
    }

    const highlightMetric = (val: number | string, type: 'time'|'nodes'|'connections') => {
        const isSelected = sortBy === type || (sortBy === 'default' && type === 'time');
        return (
            <div className={`flex flex-col items-center ${isSelected ? 'opacity-100 font-bold scale-110' : 'opacity-60'} transition-all`}>
                <span className="text-[10px] uppercase">{type}</span>
                <span className="text-sm font-mono">{val}</span>
            </div>
        );
    };

    return (
        <div className="flex flex-col items-center justify-end flex-1 max-w-[200px]">
            <div className="flex flex-col items-center mb-4 z-10 animate-in fade-in slide-in-from-bottom-4 duration-700">
                {icon}
                <div className="font-bold text-white text-lg truncate w-full text-center px-2">{entry.playerName}</div>
                <div className="text-xs text-slate-400">{entry.levelName}</div>
            </div>
            
            <div className={`w-full ${heightClass} bg-gradient-to-b ${colorClass} backdrop-blur-md rounded-t-2xl border-t border-x relative flex flex-col justify-end p-4 transition-all hover:brightness-110`}>
                <div className="absolute inset-0 bg-white/5 opacity-10"></div>
                <div className="absolute -top-3 left-1/2 -translate-x-1/2 bg-slate-950 border border-slate-700 rounded-full w-8 h-8 flex items-center justify-center font-bold text-xs z-20">
                    {rank}
                </div>
                
                <div className="flex justify-between items-end gap-2 text-slate-200 z-10">
                    {highlightMetric(entry.nodeCount, 'nodes')}
                    {highlightMetric(formatTime(entry.time), 'time')}
                    {highlightMetric(entry.connectionCount, 'connections')}
                </div>
            </div>
        </div>
    );
  };

  return (
    <div className="h-full overflow-y-auto custom-scrollbar p-6 lg:p-8">
    <div className="max-w-7xl mx-auto space-y-6 flex flex-col h-full">
      {/* Header */}
      <div className="flex flex-col md:flex-row justify-between md:items-center gap-4 shrink-0">
        <div>
          <h1 className="text-3xl font-bold text-slate-100 flex items-center gap-3">
            <Trophy className="text-amber-400" size={32} /> Global Leaderboards
          </h1>
          <p className="text-slate-400 text-sm mt-1 ml-1">Top engineering solutions across the network</p>
        </div>
        
        {user?.role === 'Admin' && (
           <button 
             onClick={() => setAdminModalOpen(true)}
             className="flex items-center gap-2 px-4 py-2 bg-purple-600 hover:bg-purple-500 text-white rounded-lg transition-colors shadow-lg shadow-purple-500/20 text-sm font-bold"
           >
             <Plus size={16} /> Register Level
           </button>
        )}
      </div>

      {/* Control Bar */}
      <div className="bg-slate-900/80 backdrop-blur-md border border-slate-800 p-2 rounded-2xl flex flex-col md:flex-row gap-4 items-center justify-between shrink-0 shadow-xl">
             
             <div className="flex bg-slate-950 rounded-xl p-1 border border-slate-800/50 w-full md:w-auto">
                 <button 
                    onClick={() => setViewMode('Global')}
                    className={`flex-1 md:flex-none px-6 py-2 rounded-lg text-sm font-bold transition-all flex items-center justify-center gap-2 ${
                        viewMode === 'Global' ? 'bg-slate-800 text-white shadow-lg' : 'text-slate-500 hover:text-slate-300'
                    }`}
                 >
                    <Crown size={16} /> Global
                 </button>
                 <button 
                    onClick={() => setViewMode('Personal')}
                    className={`flex-1 md:flex-none px-6 py-2 rounded-lg text-sm font-bold transition-all flex items-center justify-center gap-2 ${
                        viewMode === 'Personal' ? 'bg-brand-600 text-white shadow-lg shadow-brand-500/20' : 'text-slate-500 hover:text-slate-300'
                    }`}
                 >
                    <User size={16} /> My Scores
                 </button>
             </div>

             <div className="flex items-center gap-4 w-full md:w-auto">
                 <div className="relative flex-1 md:w-64 group">
                     <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors" size={16} />
                     <input 
                        list="level-options"
                        placeholder="Filter by Level..."
                        className="w-full bg-slate-950 border border-slate-800 rounded-xl py-2.5 pl-10 pr-4 text-slate-200 text-sm focus:outline-none focus:border-brand-500/50 focus:ring-1 focus:ring-brand-500/50 transition-all"
                        value={selectedLevel}
                        onChange={(e) => setSelectedLevel(e.target.value)}
                     />
                     <datalist id="level-options">
                         {availableLevels.map(lvl => <option key={lvl} value={lvl} />)}
                     </datalist>
                 </div>
                 
                 <div className="flex items-center bg-slate-950 rounded-xl border border-slate-800 p-1">
                     <button 
                        onClick={() => setSortBy('default')}
                        title="Sort by Efficiency"
                        className={`p-2.5 rounded-lg transition-colors ${sortBy === 'default' ? 'bg-slate-800 text-white shadow' : 'text-slate-500 hover:text-slate-300'}`}
                     >
                        <Zap size={18} />
                     </button>
                     <div className="w-px h-6 bg-slate-800 mx-1"></div>
                     <button 
                        onClick={() => setSortBy('time')}
                        title="Sort by Time"
                        className={`p-2.5 rounded-lg transition-colors ${sortBy === 'time' ? 'bg-slate-800 text-brand-400 shadow' : 'text-slate-500 hover:text-slate-300'}`}
                     >
                        <Timer size={18} />
                     </button>
                     <button 
                        onClick={() => setSortBy('nodes')}
                        title="Sort by Nodes"
                        className={`p-2.5 rounded-lg transition-colors ${sortBy === 'nodes' ? 'bg-slate-800 text-cyan-400 shadow' : 'text-slate-500 hover:text-slate-300'}`}
                     >
                        <Cpu size={18} />
                     </button>
                     <button 
                        onClick={() => setSortBy('connections')}
                        title="Sort by Connections"
                        className={`p-2.5 rounded-lg transition-colors ${sortBy === 'connections' ? 'bg-slate-800 text-fuchsia-400 shadow' : 'text-slate-500 hover:text-slate-300'}`}
                     >
                        <Network size={18} />
                     </button>
                 </div>
                 
                 <button 
                    onClick={() => fetchData(true)}
                    className="p-3 bg-slate-900 hover:bg-slate-800 text-slate-300 rounded-xl transition-all border border-slate-800 hover:border-slate-700 shadow-sm"
                    title="Refresh List"
                 >
                    <RefreshCw size={16} className={refreshing ? 'animate-spin text-brand-500' : ''} />
                 </button>
             </div>
      </div>

      {loading ? (
        <div className="flex-1 flex items-center justify-center">
            <div className="flex flex-col items-center gap-4">
                <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand-500"></div>
                <div className="text-slate-500 font-mono text-sm">Calculating rankings...</div>
            </div>
        </div>
      ) : sortedEntries.length === 0 ? (
        <div className="flex-1 flex flex-col items-center justify-center text-slate-500">
            <div className="w-20 h-20 bg-slate-900 rounded-full flex items-center justify-center mb-6 border border-slate-800">
                <LayoutList size={40} className="opacity-20" />
            </div>
            <p className="text-lg font-medium text-slate-400">No leaderboard entries found.</p>
            <p className="text-sm">Be the first to submit a solution!</p>
        </div>
      ) : (
        <div className="flex-1 min-h-0 flex flex-col gap-6">
            
            {/* Podium Section - Only show if we have entries and it makes sense (e.g. not user specific view unless they want to see their top) */}
            {sortedEntries.length >= 1 && (
                <div className="shrink-0 pt-8 pb-4 flex justify-center items-end gap-4 md:gap-8 px-4">
                    <PodiumStep entry={sortedEntries[1]} rank={2} />
                    <PodiumStep entry={sortedEntries[0]} rank={1} />
                    <PodiumStep entry={sortedEntries[2]} rank={3} />
                </div>
            )}

            {/* List Section */}
            <div className="bg-slate-900/50 border border-slate-800 rounded-2xl overflow-hidden shadow-2xl flex-1 flex flex-col min-h-[300px]">
                  <div className="overflow-y-auto custom-scrollbar flex-1">
                      <table className="w-full text-left border-collapse">
                          <thead className="sticky top-0 bg-slate-950/95 backdrop-blur z-20 shadow-sm border-b border-slate-800">
                              <tr className="text-xs font-bold text-slate-500 uppercase tracking-wider">
                                  <th className="px-6 py-4 w-24 text-center">Rank</th>
                                  <th className="px-6 py-4">Player</th>
                                  <th className="px-6 py-4">Level</th>
                                  <th className="px-6 py-4 text-center">Time</th>
                                  <th className="px-6 py-4 text-center">Nodes</th>
                                  <th className="px-6 py-4 text-center">Conn</th>
                                  {user?.role === 'Admin' && <th className="px-6 py-4 text-right">Actions</th>}
                              </tr>
                          </thead>
                          <tbody className="divide-y divide-slate-800/50">
                              {sortedEntries.map((entry, idx) => {
                                  const rank = idx + 1;
                                  const isUser = entry.playerName === user?.username;
                                  
                                  return (
                                  <tr key={idx} className={`group transition-colors ${isUser ? 'bg-brand-900/10 hover:bg-brand-900/20' : 'hover:bg-slate-800/30'}`}>
                                      <td className="px-6 py-4 text-center">
                                          <div className={`font-mono font-bold ${rank <= 3 ? 'text-amber-400' : 'text-slate-500'}`}>
                                              #{rank}
                                          </div>
                                      </td>
                                      <td className="px-6 py-4">
                                          <div className="flex items-center gap-3">
                                              <div className={`w-8 h-8 rounded-full flex items-center justify-center border text-xs font-bold ${
                                                  isUser 
                                                  ? 'bg-brand-500 text-slate-950 border-brand-400' 
                                                  : 'bg-slate-800 text-slate-400 border-slate-700'
                                              }`}>
                                                  {entry.playerName.charAt(0).toUpperCase()}
                                              </div>
                                              <div className={`font-medium ${isUser ? 'text-brand-400' : 'text-slate-200'}`}>
                                                  {entry.playerName}
                                                  {isUser && <span className="ml-2 text-[10px] bg-brand-500/20 text-brand-400 px-1.5 py-0.5 rounded border border-brand-500/30">YOU</span>}
                                              </div>
                                          </div>
                                      </td>
                                      <td className="px-6 py-4 text-slate-400 text-sm">
                                          {entry.levelName}
                                      </td>
                                      
                                      <td className="px-6 py-4 text-center">
                                          <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md border font-mono text-xs ${
                                              sortBy === 'time' 
                                              ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400 font-bold' 
                                              : 'bg-slate-950 border-slate-800 text-slate-400'
                                          }`}>
                                              <Timer size={12} />
                                              {formatTime(entry.time)}
                                          </div>
                                      </td>
                                      <td className="px-6 py-4 text-center">
                                           <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md border font-mono text-xs ${
                                              sortBy === 'nodes' 
                                              ? 'bg-cyan-500/10 border-cyan-500/20 text-cyan-400 font-bold' 
                                              : 'bg-slate-950 border-slate-800 text-slate-400'
                                          }`}>
                                              <Cpu size={12} />
                                              {entry.nodeCount}
                                          </div>
                                      </td>
                                      <td className="px-6 py-4 text-center">
                                           <div className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md border font-mono text-xs ${
                                              sortBy === 'connections' 
                                              ? 'bg-fuchsia-500/10 border-fuchsia-500/20 text-fuchsia-400 font-bold' 
                                              : 'bg-slate-950 border-slate-800 text-slate-400'
                                          }`}>
                                              <Network size={12} />
                                              {entry.connectionCount}
                                          </div>
                                      </td>
                                      
                                      {user?.role === 'Admin' && (
                                        <td className="px-6 py-4 text-right">
                                            <button 
                                                onClick={() => confirmDelete(entry.playerName, entry.levelName)}
                                                className="p-2 text-slate-500 hover:text-red-400 hover:bg-red-500/10 rounded-lg transition-colors opacity-0 group-hover:opacity-100 focus:opacity-100"
                                                title="Delete Submission"
                                            >
                                                <Trash2 size={16} />
                                            </button>
                                        </td>
                                      )}
                                  </tr>
                              )})}
                          </tbody>
                      </table>
                  </div>
            </div>
        </div>
      )}

      <Modal
        isOpen={isAdminModalOpen}
        onClose={() => setAdminModalOpen(false)}
        title="Register Official Level"
      >
         <form onSubmit={handleRegisterLevel} className="space-y-4 text-sm">
             <div className="p-3 bg-purple-500/10 border border-purple-500/20 rounded-lg text-purple-300 text-xs mb-4">
                 Adding a level here makes it appear in the global leaderboard tracking system. 
                 Ensure the name matches the Game Client's internal level name exactly.
             </div>

             <div>
                 <label className="block text-slate-400 mb-1 uppercase text-xs font-bold">Level Name</label>
                 <input 
                    required
                    value={newLevelName}
                    onChange={(e) => setNewLevelName(e.target.value)}
                    placeholder="e.g. Palindrome"
                    className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white focus:border-purple-500 outline-none"
                 />
             </div>

             <div>
                 <label className="block text-slate-400 mb-1 uppercase text-xs font-bold">Category</label>
                 <select
                    value={newLevelCategory}
                    onChange={(e) => setNewLevelCategory(e.target.value)}
                    className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white focus:border-purple-500 outline-none"
                 >
                     <option value="Tutorial">Tutorial</option>
                     <option value="Starter">Starter</option>
                     <option value="Medium">Medium</option>
                     <option value="Hard">Hard</option>
                     <option value="Workshop">Workshop</option>
                 </select>
             </div>

             <div>
                 <label className="block text-slate-400 mb-1 uppercase text-xs font-bold">Workshop Item ID (Optional)</label>
                 <input 
                    type="number"
                    value={newLevelWorkshopId}
                    onChange={(e) => setNewLevelWorkshopId(e.target.value)}
                    placeholder="Linked Workshop Item ID"
                    className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white focus:border-purple-500 outline-none"
                 />
             </div>

             <div className="pt-4 flex justify-end gap-3">
                 <button 
                    type="button"
                    onClick={() => setAdminModalOpen(false)}
                    className="px-4 py-2 text-slate-400 hover:text-white"
                 >
                    Cancel
                 </button>
                 <button 
                    type="submit"
                    disabled={submittingLevel}
                    className="px-6 py-2 bg-purple-600 hover:bg-purple-500 text-white rounded-lg font-bold disabled:opacity-50"
                 >
                    {submittingLevel ? 'Registering...' : 'Register Level'}
                 </button>
             </div>
         </form>
      </Modal>
      
      <ConfirmDialog 
        isOpen={deleteConfirmOpen}
        title="Delete Leaderboard Entry?"
        message={`Are you sure you want to delete the submission by ${submissionToDelete?.player} for level "${submissionToDelete?.level}"? This action cannot be undone.`}
        confirmText="Delete Submission"
        isDestructive={true}
        onCancel={() => setDeleteConfirmOpen(false)}
        onConfirm={handleExecuteDelete}
      />
    </div>
    </div>
  );
};
