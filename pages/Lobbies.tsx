
import React, { useEffect, useState, useContext } from 'react';
import { api } from '../services/api';
import { Lobby } from '../types';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { 
  Search, Trash2, Server, Lock, Globe, Users, RefreshCw, 
  Copy, Check, Crown, Play, Activity, Cpu
} from 'lucide-react';
import { UserContext } from '../App';

export const Lobbies: React.FC = () => {
  const { user } = useContext(UserContext);
  const [lobbies, setLobbies] = useState<Lobby[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [error, setError] = useState('');
  const [copiedCode, setCopiedCode] = useState<string | null>(null);

  // Confirm Dialog State
  const [confirmState, setConfirmState] = useState<{
    isOpen: boolean;
    type: 'delete' | 'kick' | null;
    lobbyCode: string | null;
    playerName: string | null;
  }>({ isOpen: false, type: null, lobbyCode: null, playerName: null });

  const fetchLobbies = async () => {
    try {
      setLoading(true);
      const data = await api.lobbies.getAll();
      setLobbies(data);
    } catch (e) {
      setError('Failed to load lobbies');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLobbies();
    const interval = setInterval(fetchLobbies, 10000);
    return () => clearInterval(interval);
  }, []);

  const handleCopyCode = (code: string) => {
    navigator.clipboard.writeText(code);
    setCopiedCode(code);
    setTimeout(() => setCopiedCode(null), 2000);
  };

  const promptDelete = (code: string) => {
    setConfirmState({ isOpen: true, type: 'delete', lobbyCode: code, playerName: null });
  };

  const promptKick = (code: string, player: string) => {
    setConfirmState({ isOpen: true, type: 'kick', lobbyCode: code, playerName: player });
  };

  const handleConfirmAction = async () => {
    const { type, lobbyCode, playerName } = confirmState;
    if (!lobbyCode) return;

    try {
        if (type === 'delete') {
            await api.lobbies.delete(lobbyCode);
            setLobbies(prev => prev.filter(l => l.code !== lobbyCode));
        } else if (type === 'kick' && playerName) {
            await api.lobbies.kick(lobbyCode, playerName);
            setLobbies(prev => prev.map(l => {
                if (l.code === lobbyCode) {
                    return {
                        ...l,
                        lobbyPlayers: (l.lobbyPlayers || []).filter(p => p !== playerName)
                    };
                }
                return l;
            }));
        }
    } catch (e) {
        console.error("Action failed");
    } finally {
        setConfirmState({ isOpen: false, type: null, lobbyCode: null, playerName: null });
    }
  };

  const filteredLobbies = lobbies.filter(l => 
    l.name.toLowerCase().includes(searchTerm.toLowerCase()) || 
    l.code.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalPlayers = lobbies.reduce((acc, l) => acc + (l.lobbyPlayers?.length || 0), 0);

  return (
    <div className="h-full overflow-y-auto custom-scrollbar p-6 lg:p-8">
    <div className="max-w-[1600px] mx-auto space-y-8 animate-in fade-in duration-500">
      
      {/* Header & Stats */}
      <div className="flex flex-col md:flex-row justify-between md:items-end gap-6 pb-6 border-b border-slate-800">
        <div>
          <h1 className="text-3xl font-bold text-slate-100 flex items-center gap-3">
            <Server className="text-emerald-400" size={32} /> Multiplayer Lobbies
          </h1>
          <p className="text-slate-400 text-sm mt-2 max-w-lg">
            Join active sessions via the Game Client to collaborate on machines or compete in challenges.
          </p>
        </div>
        
        <div className="flex gap-4">
            <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 flex items-center gap-4 min-w-[140px]">
                <div className="p-2 bg-slate-800 rounded-lg text-slate-400">
                    <Activity size={20} />
                </div>
                <div>
                    <div className="text-xl font-bold text-white font-mono">{lobbies.length}</div>
                    <div className="text-[10px] text-slate-500 uppercase font-bold tracking-wider">Active Sessions</div>
                </div>
            </div>
            <div className="bg-slate-900 border border-slate-800 rounded-xl px-5 py-3 flex items-center gap-4 min-w-[140px]">
                <div className="p-2 bg-slate-800 rounded-lg text-slate-400">
                    <Users size={20} />
                </div>
                <div>
                    <div className="text-xl font-bold text-emerald-400 font-mono">{totalPlayers}</div>
                    <div className="text-[10px] text-slate-500 uppercase font-bold tracking-wider">Players Online</div>
                </div>
            </div>
        </div>
      </div>

      {/* Controls */}
      <div className="flex flex-col md:flex-row gap-4 items-center justify-between">
          <div className="relative w-full md:w-96 group">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-emerald-400 transition-colors" size={16} />
            <input 
                type="text"
                placeholder="Search by name or code..."
                className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-4 text-slate-200 text-sm focus:outline-none focus:border-emerald-500/50 focus:ring-1 focus:ring-emerald-500/50 transition-all shadow-inner"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
            />
          </div>
          <button 
                onClick={fetchLobbies} 
                className="flex items-center gap-2 px-4 py-3 bg-slate-900 hover:bg-slate-800 text-slate-300 rounded-xl transition-all border border-slate-800 hover:border-slate-700 shadow-sm font-medium text-sm"
          >
                <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
                Refresh List
          </button>
      </div>

      {/* Grid */}
      {loading && lobbies.length === 0 ? (
        <div className="flex justify-center py-32">
             <div className="flex flex-col items-center gap-4">
                <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-emerald-500"></div>
                <div className="text-slate-500 font-mono text-sm">Scanning network...</div>
             </div>
        </div>
      ) : error ? (
        <div className="text-red-400 text-center py-20 bg-red-500/5 border border-red-500/10 rounded-2xl">
            {error}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
            {filteredLobbies.map((lobby) => {
              const players = lobby.lobbyPlayers || [];
              const playerCount = players.length;
              const fillPercent = (playerCount / lobby.maxPlayers) * 100;
              const isHost = user?.username === lobby.hostPlayer;
              const isAdmin = user?.role === 'Admin';
              
              return (
              <div 
                key={lobby.code} 
                className={`group relative bg-slate-950 border rounded-xl p-6 transition-all duration-300 hover:-translate-y-1 hover:shadow-2xl hover:shadow-black/50 flex flex-col ${
                    lobby.hasStarted 
                    ? 'border-emerald-500/20 hover:border-emerald-500/40' 
                    : 'border-slate-800 hover:border-brand-500/30'
                }`}
              >
                  {/* Background Pattern */}
                  <div className="absolute inset-0 opacity-0 group-hover:opacity-10 transition-opacity pointer-events-none bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-slate-700 via-slate-950 to-slate-950"></div>

                  <div className="relative z-10 flex flex-col h-full">
                      {/* Top Row */}
                      <div className="flex justify-between items-start mb-4">
                          <div className="flex flex-col">
                                <h3 className="text-lg font-bold text-slate-100 group-hover:text-white truncate max-w-[200px]" title={lobby.name}>{lobby.name}</h3>
                                <div className="flex items-center gap-2 mt-1">
                                    <div 
                                        onClick={() => handleCopyCode(lobby.code)}
                                        className="text-xs font-mono text-slate-500 bg-slate-900 px-1.5 py-0.5 rounded border border-slate-800 hover:border-slate-600 hover:text-slate-300 cursor-pointer flex items-center gap-1 transition-colors"
                                        title="Click to copy Code"
                                    >
                                        {lobby.code}
                                        {copiedCode === lobby.code ? <Check size={10} className="text-emerald-500" /> : <Copy size={10} />}
                                    </div>
                                    <span className="text-slate-600">•</span>
                                    <span className="text-xs text-slate-400">{lobby.levelName}</span>
                                </div>
                          </div>
                          
                          {lobby.hasStarted ? (
                                <div className="px-2 py-1 rounded-md bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-[10px] font-bold uppercase tracking-wider flex items-center gap-1.5 shadow-[0_0_10px_rgba(16,185,129,0.2)]">
                                    <span className="relative flex h-2 w-2">
                                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                                        <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                                    </span>
                                    Live
                                </div>
                            ) : (
                                <div className="px-2 py-1 rounded-md bg-amber-500/10 border border-amber-500/20 text-amber-400 text-[10px] font-bold uppercase tracking-wider flex items-center gap-1.5">
                                    <div className="w-2 h-2 rounded-full bg-amber-500 animate-pulse"></div>
                                    Waiting
                                </div>
                            )}
                      </div>

                      {/* Info Chips */}
                      <div className="flex gap-2 mb-6">
                           <div className={`text-xs px-2 py-1 rounded flex items-center gap-1.5 font-medium ${
                               lobby.passwordProtected 
                               ? 'bg-red-500/10 text-red-400 border border-red-500/10' 
                               : 'bg-slate-900 text-slate-400 border border-slate-800'
                           }`}>
                               {lobby.passwordProtected ? <Lock size={12} /> : <Globe size={12} />}
                               {lobby.passwordProtected ? 'Private' : 'Public'}
                           </div>
                           <div className="text-xs px-2 py-1 rounded bg-slate-900 text-slate-400 border border-slate-800 flex items-center gap-1.5 font-medium">
                               <Crown size={12} className="text-brand-500" />
                               Host: {lobby.hostPlayer}
                           </div>
                      </div>
                      
                      {/* Spacer */}
                      <div className="flex-1"></div>

                      {/* Capacity Bar */}
                      <div className="mb-4">
                           <div className="flex justify-between text-[10px] uppercase font-bold text-slate-500 mb-1.5">
                               <span>Capacity</span>
                               <span className={playerCount >= lobby.maxPlayers ? 'text-red-400' : 'text-emerald-400'}>
                                   {playerCount} / {lobby.maxPlayers}
                               </span>
                           </div>
                           <div className="w-full h-1.5 bg-slate-900 rounded-full overflow-hidden border border-slate-800">
                               <div 
                                    className={`h-full transition-all duration-500 ease-out ${
                                        playerCount >= lobby.maxPlayers ? 'bg-red-500' : 'bg-emerald-500'
                                    }`} 
                                    style={{ width: `${fillPercent}%` }}
                               ></div>
                           </div>
                      </div>

                      {/* Players & Actions */}
                      <div className="flex items-center justify-between pt-4 border-t border-slate-900 group-hover:border-slate-800 transition-colors">
                            <div className="flex -space-x-2">
                                {players.slice(0, 5).map((p, i) => (
                                    <div 
                                        key={p} 
                                        className={`w-8 h-8 rounded-full border-2 border-slate-950 flex items-center justify-center text-[10px] font-bold shadow-lg relative group/p ${
                                            p === lobby.hostPlayer ? 'bg-brand-600 text-white z-10' : 'bg-slate-800 text-slate-400'
                                        }`}
                                        title={p}
                                    >
                                        {p.charAt(0).toUpperCase()}
                                        {(isAdmin || isHost) && p !== lobby.hostPlayer && (
                                            <button 
                                                onClick={(e) => { e.stopPropagation(); promptKick(lobby.code, p); }}
                                                className="absolute -top-1 -right-1 w-3.5 h-3.5 bg-red-500 rounded-full text-white flex items-center justify-center opacity-0 group-hover/p:opacity-100 transition-opacity hover:scale-110"
                                            >
                                                &times;
                                            </button>
                                        )}
                                    </div>
                                ))}
                                {players.length > 5 && (
                                    <div className="w-8 h-8 rounded-full border-2 border-slate-950 bg-slate-900 text-slate-500 flex items-center justify-center text-[10px] font-bold">
                                        +{players.length - 5}
                                    </div>
                                )}
                            </div>

                            <div className="flex gap-2">
                                {(isAdmin || isHost) && (
                                    <button 
                                        onClick={() => promptDelete(lobby.code)}
                                        className="p-2 text-slate-500 hover:text-white hover:bg-red-500 rounded-lg transition-all"
                                        title="Shutdown Lobby"
                                    >
                                        <Trash2 size={16} />
                                    </button>
                                )}
                            </div>
                      </div>
                  </div>
              </div>
            )})}
            
            {/* Empty State */}
            {filteredLobbies.length === 0 && (
              <div className="col-span-full py-24 flex flex-col items-center justify-center text-center text-slate-500 bg-slate-900/30 border border-dashed border-slate-800 rounded-3xl">
                <div className="w-20 h-20 bg-slate-900 rounded-full flex items-center justify-center mb-6 border border-slate-800">
                    <Server size={40} className="opacity-20" />
                </div>
                <h3 className="text-lg font-bold text-slate-300">No active lobbies found</h3>
                <p className="max-w-xs mt-2 text-sm">Use the Game Client to host a new multiplayer session and it will appear here.</p>
              </div>
            )}
        </div>
      )}

      <ConfirmDialog 
        isOpen={confirmState.isOpen}
        title={confirmState.type === 'delete' ? "Shutdown Lobby?" : "Kick Player?"}
        message={confirmState.type === 'delete' 
            ? `Are you sure you want to shut down lobby ${confirmState.lobbyCode}? All players will be disconnected.` 
            : `Kick ${confirmState.playerName} from this lobby?`}
        confirmText={confirmState.type === 'delete' ? "Shutdown" : "Kick"}
        isDestructive={true}
        onCancel={() => setConfirmState({ isOpen: false, type: null, lobbyCode: null, playerName: null })}
        onConfirm={handleConfirmAction}
      />
    </div>
    </div>
  );
};
