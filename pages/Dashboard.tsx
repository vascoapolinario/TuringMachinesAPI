
import React, { useEffect, useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { 
  Trophy, Users, Sparkles, ArrowRight, Star, Cpu, Layers, Crown, 
  PlayCircle, Plus, Search, Server, Activity, Clock
} from 'lucide-react';
import { api } from '../services/api';
import { WorkshopItem, Lobby } from '../types';
import { UserContext } from '../App';

interface CreatorStats {
  name: string;
  totalItems: number;
  totalStars: number;
}

export const Dashboard: React.FC = () => {
  const { user } = useContext(UserContext);
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  
  // Data State
  const [featuredItem, setFeaturedItem] = useState<WorkshopItem | null>(null);
  const [topCreator, setTopCreator] = useState<CreatorStats | null>(null);
  const [recentItems, setRecentItems] = useState<WorkshopItem[]>([]);
  const [activeLobbies, setActiveLobbies] = useState<Lobby[]>([]);
  const [stats, setStats] = useState({
    machines: 0,
    levels: 0,
    onlinePlayers: 0,
    totalItems: 0
  });

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [itemsData, lobbiesData] = await Promise.all([
          api.workshop.getAll().catch(() => [] as WorkshopItem[]),
          api.lobbies.getAll().catch(() => [] as Lobby[]),
        ]);

        // Stats
        const machineCount = itemsData.filter(i => i.type === 'Machine').length;
        const levelCount = itemsData.filter(i => i.type === 'Level').length;
        const onlineCount = lobbiesData.reduce((acc, curr) => acc + (curr.lobbyPlayers?.length || 0), 0);

        setStats({
          machines: machineCount,
          levels: levelCount,
          onlinePlayers: onlineCount,
          totalItems: itemsData.length
        });

        // Featured Item logic
        if (itemsData.length > 0) {
            const highRated = itemsData.filter(i => i.rating >= 4);
            const pool = highRated.length > 0 ? highRated : itemsData;
            setFeaturedItem(pool[Math.floor(Math.random() * pool.length)]);
            setRecentItems([...itemsData].reverse().slice(0, 5));
        }

        // Top Creator logic
        const authorMap = new Map<string, { count: number, stars: number }>();
        itemsData.forEach(item => {
            const curr = authorMap.get(item.author) || { count: 0, stars: 0 };
            authorMap.set(item.author, {
                count: curr.count + 1,
                stars: curr.stars + item.rating
            });
        });
        
        let bestCreator: CreatorStats | null = null;
        let maxScore = -1;

        authorMap.forEach((val, key) => {
            const score = val.stars + (val.count * 0.5);
            if (score > maxScore) {
                maxScore = score;
                bestCreator = { name: key, totalItems: val.count, totalStars: val.stars };
            }
        });
        setTopCreator(bestCreator);

        setActiveLobbies(lobbiesData.filter(l => !l.hasStarted).slice(0, 4));

      } catch (e) {
        console.error("Dashboard fetch error", e);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const getGreeting = () => {
      const hour = new Date().getHours();
      if (hour < 12) return "Good morning";
      if (hour < 18) return "Good afternoon";
      return "Good evening";
  };

  if (loading) {
    return (
      <div className="flex h-full items-center justify-center">
         <div className="flex flex-col items-center gap-4">
             <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand-500"></div>
             <span className="text-slate-500 text-sm font-mono animate-pulse">Loading Turing Sandbox API...</span>
         </div>
      </div>
    );
  }

  return (
    <div className="h-full overflow-y-auto custom-scrollbar p-6 lg:p-8">
        <div className="max-w-[1600px] mx-auto space-y-8 animate-in fade-in duration-500 pb-10">
            
            {/* --- Header Section --- */}
            <header className="flex flex-col md:flex-row md:items-end justify-between gap-6 border-b border-slate-800/50 pb-6">
                <div>
                    <h1 className="text-3xl font-bold text-slate-100 tracking-tight mb-2">
                        {getGreeting()}, <span className="text-brand-400">{user?.username || 'Engineer'}</span>.
                    </h1>
                    <div className="flex items-center gap-6 text-sm text-slate-400">
                        <div className="flex items-center gap-2">
                            <Activity size={16} className="text-emerald-500" />
                            <span>Systems Operational</span>
                        </div>
                        <div className="flex items-center gap-2">
                            <Clock size={16} className="text-brand-500" />
                            <span>{new Date().toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' })}</span>
                        </div>
                    </div>
                </div>

                <div className="flex gap-4">
                     <div className="px-4 py-2 bg-slate-900 border border-slate-800 rounded-lg flex flex-col items-end min-w-[100px]">
                        <span className="text-[10px] uppercase font-bold text-slate-500">Database</span>
                        <span className="text-xl font-mono font-bold text-slate-200">{stats.totalItems}</span>
                     </div>
                     <div className="px-4 py-2 bg-slate-900 border border-slate-800 rounded-lg flex flex-col items-end min-w-[100px]">
                        <span className="text-[10px] uppercase font-bold text-slate-500">Online</span>
                        <div className="flex items-center gap-2">
                            <span className="relative flex h-2 w-2">
                                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>
                                <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>
                            </span>
                            <span className="text-xl font-mono font-bold text-slate-200">{stats.onlinePlayers}</span>
                        </div>
                     </div>
                </div>
            </header>

            {/* --- Quick Actions --- */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <button 
                    onClick={() => navigate('/simulator')}
                    className="group relative overflow-hidden p-5 rounded-2xl bg-gradient-to-br from-brand-600 to-brand-700 hover:to-brand-600 transition-all shadow-lg hover:shadow-brand-500/20 text-left border border-white/10"
                >
                     <div className="absolute top-0 right-0 p-4 opacity-10 group-hover:scale-110 transition-transform">
                        <PlayCircle size={64} />
                    </div>
                    <div className="relative z-10">
                        <div className="bg-white/20 w-10 h-10 rounded-lg flex items-center justify-center mb-3 backdrop-blur-sm">
                            <PlayCircle size={20} className="text-white" />
                        </div>
                        <h3 className="font-bold text-white text-lg">Simulator</h3>
                        <p className="text-brand-100 text-xs mt-1 font-medium">Run Tests & Visualize</p>
                    </div>
                </button>

                <button 
                    onClick={() => navigate('/workshop')}
                    className="group relative overflow-hidden p-5 rounded-2xl bg-slate-800 hover:bg-slate-750 border border-slate-700 hover:border-slate-600 transition-all text-left"
                >
                     <div className="absolute top-0 right-0 p-4 opacity-5 group-hover:opacity-10 transition-opacity">
                        <Search size={64} />
                    </div>
                    <div className="bg-slate-900 w-10 h-10 rounded-lg flex items-center justify-center mb-3 border border-slate-700 text-cyan-400">
                        <Search size={20} />
                    </div>
                    <h3 className="font-bold text-slate-200 text-lg">Workshop</h3>
                    <p className="text-slate-500 text-xs mt-1">Browse Items</p>
                </button>

                <button 
                    onClick={() => navigate('/lobbies')}
                    className="group relative overflow-hidden p-5 rounded-2xl bg-slate-800 hover:bg-slate-750 border border-slate-700 hover:border-slate-600 transition-all text-left"
                >
                     <div className="absolute top-0 right-0 p-4 opacity-5 group-hover:opacity-10 transition-opacity">
                        <Server size={64} />
                    </div>
                    <div className="bg-slate-900 w-10 h-10 rounded-lg flex items-center justify-center mb-3 border border-slate-700 text-emerald-400">
                        <Server size={20} />
                    </div>
                    <h3 className="font-bold text-slate-200 text-lg">Multiplayer</h3>
                    <p className="text-slate-500 text-xs mt-1">View Active Lobbies</p>
                </button>
            </div>

            {/* --- Bento Grid Content --- */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                
                {/* 1. Featured Spotlight (Span 2 cols) */}
                <div className="lg:col-span-2 relative group rounded-2xl overflow-hidden border border-slate-700 hover:border-brand-500/50 transition-all shadow-xl bg-slate-900">
                    {featuredItem ? (
                        <>
                            {/* Dynamic Background */}
                            <div className={`absolute inset-0 bg-gradient-to-br opacity-20 transition-opacity group-hover:opacity-30 ${
                                featuredItem.type === 'Machine' ? 'from-cyan-600 via-slate-900 to-slate-950' : 'from-fuchsia-600 via-slate-900 to-slate-950'
                            }`} />
                            
                            <div className="relative p-8 h-full flex flex-col justify-between z-10">
                                <div className="flex justify-between items-start">
                                    <div>
                                        <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-full bg-white/10 backdrop-blur border border-white/10 text-white text-[10px] font-bold uppercase tracking-wider mb-4">
                                            <Sparkles size={12} className="text-amber-400" /> Community Spotlight
                                        </div>
                                        <h2 className="text-3xl font-bold text-white mb-2 group-hover:text-brand-200 transition-colors">{featuredItem.name}</h2>
                                        <p className="text-slate-300 max-w-xl line-clamp-2">{featuredItem.description}</p>
                                    </div>
                                    <div className={`hidden md:flex p-4 rounded-xl border border-white/10 backdrop-blur-sm ${
                                        featuredItem.type === 'Machine' ? 'bg-cyan-500/10 text-cyan-400' : 'bg-fuchsia-500/10 text-fuchsia-400'
                                    }`}>
                                        {featuredItem.type === 'Machine' ? <Cpu size={32} /> : <Layers size={32} />}
                                    </div>
                                </div>

                                <div className="flex items-center justify-between mt-8 pt-6 border-t border-white/10">
                                    <div className="flex items-center gap-6">
                                        <div className="flex items-center gap-2">
                                            <div className="w-8 h-8 rounded-full bg-slate-800 flex items-center justify-center text-slate-400 border border-slate-600">
                                                <span className="text-xs font-bold">{featuredItem.author.charAt(0).toUpperCase()}</span>
                                            </div>
                                            <div className="flex flex-col">
                                                <span className="text-[10px] uppercase text-slate-400 font-bold">Creator</span>
                                                <span className="text-sm font-medium text-slate-200">{featuredItem.author}</span>
                                            </div>
                                        </div>
                                        <div className="hidden sm:flex flex-col">
                                            <span className="text-[10px] uppercase text-slate-400 font-bold">Rating</span>
                                            <div className="flex items-center gap-1 text-amber-400 font-bold text-sm">
                                                <Star size={14} fill="currentColor" /> {featuredItem.rating.toFixed(1)}
                                            </div>
                                        </div>
                                    </div>

                                    <button 
                                        onClick={() => navigate(featuredItem.type === 'Level' ? `/simulator?levelId=${featuredItem.id}` : `/simulator?machineId=${featuredItem.id}`)}
                                        className="bg-white text-slate-950 hover:bg-brand-50 font-bold py-2.5 px-6 rounded-lg inline-flex items-center gap-2 transition-colors"
                                    >
                                        <PlayCircle size={18} />
                                        Open in Simulator
                                    </button>
                                </div>
                            </div>
                        </>
                    ) : (
                        <div className="p-8 text-center text-slate-500">Loading Spotlight...</div>
                    )}
                </div>

                {/* 2. Top Engineer Profile */}
                <div className="bg-slate-900 border border-slate-800 rounded-2xl p-6 flex flex-col relative overflow-hidden hover:border-purple-500/30 transition-colors group">
                    <div className="absolute top-0 right-0 p-4 opacity-5 group-hover:opacity-10 transition-opacity">
                        <Crown size={100} className="rotate-12" />
                    </div>
                    
                    <div className="flex items-center gap-2 mb-6">
                        <Crown size={20} className="text-purple-400" />
                        <h3 className="font-bold text-slate-200 text-sm uppercase tracking-wider">Top Engineer</h3>
                    </div>

                    {topCreator ? (
                        <div className="flex-1 flex flex-col items-center justify-center text-center z-10">
                            <div className="w-20 h-20 rounded-full bg-slate-800 border-4 border-purple-500/20 flex items-center justify-center text-purple-400 shadow-xl mb-3 group-hover:scale-105 transition-transform">
                                <span className="text-3xl font-bold">{topCreator.name.charAt(0).toUpperCase()}</span>
                            </div>
                            <h2 className="text-xl font-bold text-white">{topCreator.name}</h2>
                            <p className="text-purple-300 text-xs mb-6 font-mono">Master Architect</p>
                            
                            <div className="flex items-center justify-center gap-2 w-full">
                                <div className="bg-slate-950 rounded-lg p-2.5 border border-slate-800 flex-1">
                                    <div className="text-sm font-bold text-white">{topCreator.totalItems}</div>
                                    <div className="text-[10px] text-slate-500 uppercase">Items</div>
                                </div>
                                <div className="bg-slate-950 rounded-lg p-2.5 border border-slate-800 flex-1">
                                    <div className="text-sm font-bold text-amber-400 flex items-center justify-center gap-1">
                                        {topCreator.totalStars.toFixed(0)} <Star size={10} fill="currentColor" />
                                    </div>
                                    <div className="text-[10px] text-slate-500 uppercase">Stars</div>
                                </div>
                            </div>
                        </div>
                    ) : (
                        <div className="flex-1 flex items-center justify-center text-slate-500 text-sm">No data</div>
                    )}
                </div>

                {/* 3. Recent Workshop Additions (Vertical List) */}
                <div className="lg:col-span-1 bg-slate-900 border border-slate-800 rounded-2xl overflow-hidden flex flex-col">
                     <div className="p-4 border-b border-slate-800 flex justify-between items-center bg-slate-950/30">
                        <h3 className="font-bold text-slate-200 text-sm flex items-center gap-2">
                            <Clock size={16} className="text-brand-400" /> New Arrivals
                        </h3>
                        <button onClick={() => navigate('/workshop')} className="text-xs text-brand-400 hover:text-brand-300 font-medium">View All</button>
                     </div>
                     <div className="flex-1 overflow-y-auto p-2 space-y-1">
                        {recentItems.map(item => (
                            <div 
                                key={item.id} 
                                onClick={() => navigate(`/workshop?author=${item.author}`)}
                                className="p-3 rounded-lg hover:bg-slate-800 transition-colors cursor-pointer group flex items-center gap-3"
                            >
                                <div className={`p-2 rounded-md ${item.type === 'Machine' ? 'bg-cyan-500/10 text-cyan-400' : 'bg-fuchsia-500/10 text-fuchsia-400'}`}>
                                    {item.type === 'Machine' ? <Cpu size={16} /> : <Layers size={16} />}
                                </div>
                                <div className="flex-1 min-w-0">
                                    <h4 className="text-sm font-medium text-slate-200 truncate group-hover:text-white transition-colors">{item.name}</h4>
                                    <p className="text-xs text-slate-500 truncate">by {item.author}</p>
                                </div>
                                <div className="text-xs text-slate-600 group-hover:text-slate-400">
                                    <ArrowRight size={14} />
                                </div>
                            </div>
                        ))}
                     </div>
                </div>

                {/* 4. Live Servers Monitor */}
                <div className="lg:col-span-2 bg-slate-900 border border-slate-800 rounded-2xl p-6">
                    <div className="flex items-center justify-between mb-4">
                        <div className="flex items-center gap-2">
                             <Server size={20} className="text-emerald-400" />
                             <h3 className="font-bold text-slate-200 text-sm uppercase tracking-wider">Live Channels</h3>
                        </div>
                        <span className="text-xs text-emerald-500 font-mono animate-pulse">● LIVE</span>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                         {activeLobbies.length === 0 ? (
                             <div className="col-span-2 text-center py-8 text-slate-500 text-sm border border-dashed border-slate-800 rounded-xl bg-slate-950/50">
                                 No active lobbies found. <br/>
                                 <button onClick={() => navigate('/lobbies')} className="mt-2 text-brand-400 hover:underline">View Lobby Browser</button>
                             </div>
                         ) : (
                             activeLobbies.map(lobby => {
                                 const fillPercent = ((lobby.lobbyPlayers?.length || 0) / lobby.maxPlayers) * 100;
                                 return (
                                     <div key={lobby.code} className="bg-slate-950 border border-slate-800 p-4 rounded-xl flex flex-col gap-3 hover:border-slate-700 transition-colors">
                                         <div className="flex justify-between items-start">
                                             <div>
                                                 <h4 className="font-bold text-slate-200 text-sm">{lobby.name}</h4>
                                                 <div className="text-xs text-slate-500 font-mono mt-0.5">{lobby.code} • {lobby.levelName}</div>
                                             </div>
                                         </div>
                                         
                                         {/* Capacity Bar */}
                                         <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden flex">
                                             <div className="h-full bg-emerald-500 transition-all duration-500" style={{ width: `${fillPercent}%` }}></div>
                                         </div>
                                         <div className="flex justify-between text-[10px] text-slate-500 font-mono uppercase">
                                             <span>Capacity</span>
                                             <span>{(lobby.lobbyPlayers?.length || 0)} / {lobby.maxPlayers}</span>
                                         </div>
                                     </div>
                                 );
                             })
                         )}
                    </div>
                </div>

            </div>
        </div>
    </div>
  );
};
