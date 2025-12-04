
import React, { useEffect, useState, useContext } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../services/api';
import { WorkshopItem, WorkshopItemDetail } from '../types';
import { Modal } from '../components/Modal';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { 
  Search, Trash2, Box, Star, User, Code, Filter, Layers, Cpu, 
  ArrowLeft, LayoutGrid, ListFilter, Tag, PlayCircle, Disc, 
  Sparkles, Zap, Download, ArrowRight, X, RefreshCw
} from 'lucide-react';
import { UserContext } from '../App';

export const Workshop: React.FC = () => {
  const { user } = useContext(UserContext);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  
  const [items, setItems] = useState<WorkshopItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState('');

  // Filter States
  const [category, setCategory] = useState<'All' | 'Machine' | 'Level'>('All');
  const [sortBy, setSortBy] = useState<'newest' | 'rating' | 'popular'>('newest');
  const [levelMode, setLevelMode] = useState<'All' | 'accept' | 'transform'>('All');
  const [tapeFilter, setTapeFilter] = useState<'All' | '1' | '2'>('All');
  
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedAuthor, setSelectedAuthor] = useState<string | null>(null);
  
  // Modal State
  const [selectedItem, setSelectedItem] = useState<WorkshopItemDetail | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [inspectLoading, setInspectLoading] = useState(false);

  // Delete Confirmation State
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [itemToDelete, setItemToDelete] = useState<number | null>(null);

  useEffect(() => {
    const authorParam = searchParams.get('author');
    if (authorParam) setSelectedAuthor(authorParam);
    fetchItems();
  }, [searchParams]);

  const fetchItems = async (force = false) => {
    try {
      if (force) setRefreshing(true);
      else setLoading(true);
      
      const data = await api.workshop.getAll(force);
      setItems(data);
    } catch (e) {
      setError('Failed to load workshop items');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const handleInspect = async (id: number) => {
    setIsModalOpen(true);
    setInspectLoading(true);
    try {
      const details = await api.workshop.get(id);
      setSelectedItem(details);
    } catch (e) {
      console.error("Failed to fetch item details");
      setIsModalOpen(false);
    } finally {
      setInspectLoading(false);
    }
  };

  const confirmDelete = (id: number) => {
    setItemToDelete(id);
    setConfirmOpen(true);
  };

  const handleExecuteDelete = async () => {
    if (!itemToDelete) return;
    try {
      await api.workshop.delete(itemToDelete);
      setItems(prev => prev.filter(i => i.id !== itemToDelete));
    } catch (e) {
      console.error('Failed to delete item.');
    } finally {
      setConfirmOpen(false);
      setItemToDelete(null);
    }
  };

  const handleRate = async (id: number, rating: number) => {
    try {
        await api.workshop.rate(id, rating);
        setItems(prev => prev.map(item => {
            if (item.id === id) {
                return { ...item, rating: rating, userRating: rating };
            }
            return item;
        }));
    } catch (e) {
        console.error("Failed to rate");
    }
  };

  const handleSimulate = (item: WorkshopItem) => {
    const param = item.type === 'Level' ? 'levelId' : 'machineId';
    navigate(`/simulator?${param}=${item.id}`);
  };

  // Filtering Logic
  const filteredItems = items
    .filter(i => {
        if (selectedAuthor && i.author !== selectedAuthor) return false;
        if (category !== 'All' && i.type !== category) return false;
        if (category === 'Level' && levelMode !== 'All' && i.mode !== levelMode) return false;
        if (tapeFilter !== 'All') {
           if (i.twoTapes !== undefined) {
               if (tapeFilter === '2' && !i.twoTapes) return false;
               if (tapeFilter === '1' && i.twoTapes) return false;
           }
        }
        if (searchTerm && !i.name.toLowerCase().includes(searchTerm.toLowerCase()) && !i.author.toLowerCase().includes(searchTerm.toLowerCase())) return false;
        return true;
    })
    .sort((a, b) => {
        if (sortBy === 'rating') return (b.rating || 0) - (a.rating || 0);
        if (sortBy === 'popular') return b.subscribers - a.subscribers;
        return b.id - a.id;
    });

  const parseAlphabet = (json?: string) => {
    if (!json) return [];
    try {
        return JSON.parse(json);
    } catch {
        return [];
    }
  };

  const heroItem = !selectedAuthor && !searchTerm && filteredItems.length > 0 
    ? filteredItems.reduce((prev, current) => (prev.rating > current.rating) ? prev : current)
    : null;

  return (
    <div className="flex flex-col lg:flex-row gap-6 h-full p-6">
      
      {/* --- MAIN CONTENT AREA (CENTER STAGE) --- */}
      <div className="flex-1 flex flex-col min-h-0 bg-slate-900/40 border border-slate-800 rounded-2xl overflow-hidden shadow-2xl">
        
        <div className="flex-1 overflow-y-auto custom-scrollbar p-6">
            {/* Header / Search */}
            <div className="mb-8 space-y-6">
                {selectedAuthor ? (
                    <div className="relative overflow-hidden rounded-2xl border border-slate-700 bg-slate-900 shadow-xl">
                        <div className="absolute inset-0 bg-gradient-to-r from-brand-900/80 to-slate-900 z-0"></div>
                        <div className="absolute right-0 top-0 bottom-0 w-1/3 bg-gradient-to-l from-brand-500/10 to-transparent"></div>
                        
                        <div className="relative z-10 p-8 flex items-center gap-6">
                            <div className="h-20 w-20 rounded-full bg-slate-950 border-4 border-slate-800 flex items-center justify-center text-slate-300 shadow-2xl shrink-0">
                                <User size={40} />
                            </div>
                            <div className="flex-1">
                                <div className="text-brand-400 font-bold uppercase text-xs tracking-widest mb-1">Workshop Creator</div>
                                <h1 className="text-3xl font-bold text-white mb-2">{selectedAuthor}</h1>
                                <div className="flex gap-4 text-sm text-slate-400">
                                    <span><span className="text-white font-bold">{filteredItems.length}</span> Items</span>
                                    <span><span className="text-white font-bold">{filteredItems.reduce((a,b) => a + b.subscribers, 0)}</span> Subscribers</span>
                                </div>
                            </div>
                             <button 
                                onClick={() => {
                                    setSelectedAuthor(null);
                                    navigate('/workshop'); 
                                }}
                                className="group flex items-center gap-2 text-sm text-slate-400 hover:text-white transition-colors font-medium p-3 bg-slate-950/50 border border-slate-800 rounded-xl hover:border-slate-700"
                            >
                                <X size={16} />
                                Clear Filter
                            </button>
                        </div>
                    </div>
                ) : (
                    <div className="flex flex-col md:flex-row justify-between md:items-end gap-4 pb-4 border-b border-slate-800">
                        <div>
                            <h1 className="text-3xl font-bold text-slate-100 tracking-tight flex items-center gap-3">
                                <Box className="text-brand-500" /> Community Workshop
                            </h1>
                            <p className="text-slate-400 text-sm mt-2 max-w-lg">
                                Discover {items.length} community-made machines and levels. Play, inspect, and learn from the best engineers.
                            </p>
                        </div>
                        <div className="flex gap-3 w-full md:w-auto">
                            <div className="relative flex-1 md:w-80">
                                <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500" size={16} />
                                <input 
                                    type="text"
                                    placeholder="Search items..."
                                    className="w-full bg-slate-950 border border-slate-800 rounded-xl py-3 pl-10 pr-4 text-slate-200 text-sm focus:outline-none focus:border-brand-500/50 focus:ring-1 focus:ring-brand-500/50 transition-all shadow-inner"
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                />
                            </div>
                            <button 
                                onClick={() => fetchItems(true)}
                                className="px-4 bg-slate-900 border border-slate-800 rounded-xl hover:bg-slate-800 transition-colors flex items-center justify-center text-slate-400 hover:text-white"
                                title="Refresh"
                            >
                                <RefreshCw size={20} className={refreshing ? 'animate-spin text-brand-500' : ''} />
                            </button>
                        </div>
                    </div>
                )}
            </div>

            {/* Featured Hero (Only on main view) */}
            {!selectedAuthor && !searchTerm && heroItem && (
                <div className="shrink-0 mb-10 rounded-2xl overflow-hidden relative group cursor-pointer border border-slate-700 hover:border-brand-500/30 transition-all shadow-2xl" onClick={() => handleSimulate(heroItem)}>
                    <div className={`absolute inset-0 bg-gradient-to-r ${heroItem.type === 'Machine' ? 'from-cyan-950 to-slate-900' : 'from-fuchsia-950 to-slate-900'}`}></div>
                    <div className="absolute inset-0 bg-[url('https://grainy-gradients.vercel.app/noise.svg')] opacity-20"></div>
                    
                    <div className="relative p-8 md:p-10 flex flex-col md:flex-row gap-8 items-start md:items-center justify-between">
                        <div className="space-y-4 max-w-2xl">
                            <div className="flex items-center gap-3 mb-2">
                                <span className="px-2 py-1 bg-white/10 text-white text-[10px] font-bold uppercase tracking-widest rounded backdrop-blur-md border border-white/10">
                                    Featured Pick
                                </span>
                                <div className="flex items-center gap-1 text-amber-400">
                                    <Star size={14} fill="currentColor" />
                                    <span className="font-bold text-sm">{heroItem.rating.toFixed(1)}</span>
                                </div>
                            </div>
                            <h2 className="text-3xl md:text-4xl font-bold text-white leading-tight group-hover:text-brand-200 transition-colors">
                                {heroItem.name}
                            </h2>
                            <p className="text-slate-300 text-lg line-clamp-2">{heroItem.description}</p>
                            
                            <div className="flex items-center gap-4 pt-2">
                                <button className="bg-white text-slate-950 px-6 py-2.5 rounded-lg font-bold text-sm hover:bg-brand-50 transition-colors flex items-center gap-2 shadow-lg shadow-white/10">
                                    <PlayCircle size={18} /> Simulate Now
                                </button>
                                <div className="flex items-center gap-2 text-slate-400 text-sm pl-4 border-l border-white/10">
                                    <User size={16} /> Created by <span className="text-slate-200 font-medium hover:text-white hover:underline">{heroItem.author}</span>
                                </div>
                            </div>
                        </div>
                        
                        <div className={`hidden md:flex h-32 w-32 shrink-0 rounded-2xl items-center justify-center border-2 border-white/10 shadow-2xl rotate-3 group-hover:rotate-6 transition-transform duration-500 ${
                            heroItem.type === 'Machine' ? 'bg-cyan-500/20 text-cyan-400' : 'bg-fuchsia-500/20 text-fuchsia-400'
                        }`}>
                            {heroItem.type === 'Machine' ? <Cpu size={64} /> : <Layers size={64} />}
                        </div>
                    </div>
                </div>
            )}

            {/* Grid */}
            <div>
                {loading ? (
                    <div className="flex flex-col items-center justify-center h-64 gap-4">
                        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand-500"></div>
                        <span className="text-slate-500 text-sm">Loading workshop content...</span>
                    </div>
                ) : filteredItems.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-64 text-slate-500 border border-dashed border-slate-800 rounded-xl bg-slate-900/30">
                        <Box size={48} className="mb-4 opacity-50" />
                        <p>No items found matching your filters.</p>
                        <button onClick={() => {setCategory('All'); setSearchTerm(''); setTapeFilter('All')}} className="mt-4 text-brand-400 hover:underline text-sm">
                            Clear Filters
                        </button>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 2xl:grid-cols-3 min-[1800px]:grid-cols-4 gap-6">
                        {filteredItems.map((item) => {
                            const alphabet = parseAlphabet(item.alphabetJson);
                            const isMachine = item.type === 'Machine';
                            
                            return (
                            <div key={item.id} className="group flex flex-col bg-slate-950 border border-slate-800 hover:border-slate-600 rounded-xl overflow-hidden transition-all duration-300 hover:shadow-2xl hover:shadow-black/60 hover:-translate-y-1">
                                
                                <div className={`h-36 relative flex items-center justify-center overflow-hidden ${
                                    isMachine 
                                    ? 'bg-gradient-to-br from-cyan-950/50 to-slate-950' 
                                    : 'bg-gradient-to-br from-fuchsia-950/50 to-slate-950'
                                }`}>
                                    <div className="absolute inset-0 opacity-10" style={{backgroundImage: 'radial-gradient(circle, #fff 1px, transparent 1px)', backgroundSize: '16px 16px'}}></div>
                                    
                                    <div className={`transform transition-transform duration-500 group-hover:scale-110 drop-shadow-2xl ${
                                        isMachine ? 'text-cyan-500/80' : 'text-fuchsia-500/80'
                                    }`}>
                                        {isMachine ? <Cpu size={56} strokeWidth={1.5} /> : <Layers size={56} strokeWidth={1.5} />}
                                    </div>

                                    <div className="absolute top-3 left-3 flex gap-2">
                                        <span className={`text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded backdrop-blur-md border shadow-lg ${
                                            isMachine 
                                            ? 'bg-cyan-500/10 text-cyan-300 border-cyan-500/20' 
                                            : 'bg-fuchsia-500/10 text-fuchsia-300 border-fuchsia-500/20'
                                        }`}>
                                            {item.type}
                                        </span>
                                        {item.twoTapes !== undefined && (
                                            <span className="text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded backdrop-blur-md border bg-emerald-500/10 text-emerald-300 border-emerald-500/20 flex items-center gap-1 shadow-lg">
                                                <Disc size={10} /> {item.twoTapes ? '2 Tapes' : '1 Tape'}
                                            </span>
                                        )}
                                    </div>
                                </div>

                                <div className="p-5 flex-1 flex flex-col">
                                    <div className="mb-3">
                                        <div className="flex justify-between items-start">
                                            <h3 className="font-bold text-slate-100 truncate text-lg group-hover:text-brand-400 transition-colors" title={item.name}>{item.name}</h3>
                                        </div>
                                        <div 
                                            onClick={(e) => { e.stopPropagation(); setSelectedAuthor(item.author); }}
                                            className="text-xs text-slate-500 hover:text-slate-300 cursor-pointer flex items-center gap-1.5 mt-1 w-fit transition-colors"
                                        >
                                            <User size={12} /> {item.author}
                                        </div>
                                    </div>
                                    
                                    <p className="text-sm text-slate-400 mb-4 line-clamp-2 leading-relaxed min-h-[40px]">{item.description || "No description provided."}</p>
                                    
                                    <div className="flex items-center justify-between text-xs text-slate-500 mb-5 bg-slate-900 p-2.5 rounded-lg border border-slate-800">
                                        <div className="flex items-center gap-1.5" title="Subscribers">
                                            <Download size={12} />
                                            <span className="font-mono text-slate-300 font-bold">{item.subscribers}</span>
                                        </div>
                                        <div className="flex items-center gap-1">
                                            <div className="flex">
                                                {[1, 2, 3, 4, 5].map(star => (
                                                    <Star 
                                                        key={star} 
                                                        size={10} 
                                                        className={`${item.rating >= star ? 'text-amber-400 fill-amber-400' : 'text-slate-700'}`} 
                                                    />
                                                ))}
                                            </div>
                                        </div>
                                    </div>

                                    <div className="mt-auto pt-4 border-t border-slate-800 grid grid-cols-4 gap-2">
                                        <button 
                                            onClick={() => handleSimulate(item)}
                                            className="col-span-2 py-2 bg-slate-100 hover:bg-white text-slate-950 rounded-lg font-bold text-sm transition-colors flex items-center justify-center gap-2 shadow-lg shadow-white/5"
                                        >
                                            <PlayCircle size={16} fill="currentColor" className="text-slate-950/20" /> Simulate
                                        </button>
                                        
                                        <button 
                                            onClick={() => handleInspect(item.id)}
                                            className="col-span-1 py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white rounded-lg font-medium text-xs transition-colors flex items-center justify-center border border-slate-700"
                                            title="Inspect Code"
                                        >
                                            <Code size={16} />
                                        </button>

                                        <div className="col-span-1 flex items-center justify-center relative group/rate">
                                             <button className="w-full h-full flex items-center justify-center bg-slate-800 hover:bg-amber-500/10 hover:text-amber-400 hover:border-amber-500/30 text-slate-500 border border-slate-700 rounded-lg transition-all">
                                                <Star size={16} />
                                             </button>
                                             <div className="absolute bottom-full right-0 mb-2 hidden group-hover/rate:flex bg-slate-800 p-2 rounded-xl border border-slate-700 shadow-2xl gap-1 z-20 w-max">
                                                {[1, 2, 3, 4, 5].map(star => (
                                                    <button 
                                                        key={star}
                                                        onClick={(e) => { e.stopPropagation(); handleRate(item.id, star); }}
                                                        className="p-1 hover:text-amber-400 text-slate-600 transition-colors"
                                                    >
                                                        <Star size={16} fill={item.userRating >= star ? "currentColor" : "none"} />
                                                    </button>
                                                ))}
                                            </div>
                                        </div>
                                    </div>
                                    {(user?.role === 'Admin' || user?.username === item.author) && (
                                        <button 
                                            onClick={() => confirmDelete(item.id)}
                                            className="absolute top-4 right-4 p-2 bg-slate-950/80 hover:bg-red-500/80 text-slate-500 hover:text-white rounded-lg transition-colors opacity-0 group-hover:opacity-100 backdrop-blur"
                                            title="Delete"
                                        >
                                            <Trash2 size={16} />
                                        </button>
                                    )}
                                </div>
                            </div>
                        )})}
                    </div>
                )}
            </div>
        </div>
      </div>

      {/* --- SIDEBAR FILTERS (RIGHT SIDE) --- */}
      <div className="w-full lg:w-72 flex-shrink-0 flex flex-col gap-6 h-fit">
        <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-lg">
            <h3 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3 flex items-center gap-2">
                <Filter size={14} /> Sort Order
            </h3>
            <select 
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as any)}
                className="w-full bg-slate-950 border border-slate-800 rounded-lg px-3 py-2.5 text-slate-200 text-sm focus:outline-none focus:border-brand-500 transition-colors cursor-pointer"
            >
                <option value="newest">Most Recent</option>
                <option value="popular">Most Popular</option>
                <option value="rating">Highest Rated</option>
            </select>
        </div>

        <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-lg">
            <h3 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3 flex items-center gap-2">
                <LayoutGrid size={14} /> Item Type
            </h3>
            <div className="space-y-1">
                {(['All', 'Machine', 'Level'] as const).map(cat => (
                    <button
                        key={cat}
                        onClick={() => setCategory(cat)}
                        className={`w-full text-left px-4 py-3 rounded-lg text-sm font-medium transition-all flex items-center justify-between group ${
                            category === cat 
                            ? 'bg-brand-600 text-white shadow-lg shadow-brand-500/20' 
                            : 'text-slate-400 hover:text-white hover:bg-slate-800'
                        }`}
                    >
                        <span className="flex items-center gap-3">
                            {cat === 'All' && <ListFilter size={16} />}
                            {cat === 'Machine' && <Cpu size={16} />}
                            {cat === 'Level' && <Layers size={16} />}
                            {cat === 'All' ? 'Everything' : cat + 's'}
                        </span>
                        {category === cat && <div className="w-1.5 h-1.5 rounded-full bg-white animate-pulse"></div>}
                    </button>
                ))}
            </div>
        </div>

        {category !== 'Machine' && (
        <div className="space-y-6">
            {category === 'Level' && (
                 <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-lg animate-in slide-in-from-right-4 duration-300">
                    <h3 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3 flex items-center gap-2">
                        <Tag size={14} /> Objective Mode
                    </h3>
                    <div className="space-y-1">
                        {(['All', 'accept', 'transform'] as const).map(mode => (
                            <button
                                key={mode}
                                onClick={() => setLevelMode(mode)}
                                className={`w-full text-left px-4 py-2 rounded-lg text-sm font-medium transition-all capitalize border-l-2 ${
                                    levelMode === mode 
                                    ? 'border-indigo-500 bg-indigo-500/10 text-indigo-400' 
                                    : 'border-transparent text-slate-400 hover:text-slate-200 hover:bg-slate-800'
                                }`}
                            >
                                {mode}
                            </button>
                        ))}
                    </div>
                </div>
            )}
            
            <div className="bg-slate-900 border border-slate-800 rounded-xl p-4 shadow-lg animate-in slide-in-from-right-4 duration-300">
                <h3 className="text-xs font-bold text-slate-500 uppercase tracking-wider mb-3 flex items-center gap-2">
                    <Disc size={14} /> Tapes Config
                </h3>
                <div className="flex bg-slate-950 p-1 rounded-lg border border-slate-800">
                    {[
                        { id: 'All', label: 'Any' },
                        { id: '1', label: '1 Tape' },
                        { id: '2', label: '2 Tapes' },
                    ].map(opt => (
                        <button
                            key={opt.id}
                            onClick={() => setTapeFilter(opt.id as any)}
                            className={`flex-1 py-2 rounded-md text-xs font-bold transition-all ${
                                tapeFilter === opt.id 
                                ? 'bg-slate-800 text-white shadow ring-1 ring-slate-700' 
                                : 'text-slate-500 hover:text-slate-300'
                            }`}
                        >
                            {opt.label}
                        </button>
                    ))}
                </div>
            </div>
        </div>
        )}
      </div>

      <Modal 
        isOpen={isModalOpen} 
        onClose={() => setIsModalOpen(false)} 
        title={selectedItem ? selectedItem.name : 'Item Details'}
      >
        {inspectLoading ? (
             <div className="flex justify-center py-20">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand-500"></div>
             </div>
        ) : selectedItem ? (
            <div className="space-y-6">
                <div className="grid grid-cols-2 gap-4 text-sm">
                    <div className="p-4 bg-slate-950 rounded-lg border border-slate-800">
                        <span className="text-slate-500 block text-xs font-medium uppercase mb-1">Author</span>
                        <span className="text-slate-200">{selectedItem.author}</span>
                    </div>
                    <div className="p-4 bg-slate-950 rounded-lg border border-slate-800">
                        <span className="text-slate-500 block text-xs font-medium uppercase mb-1">Type</span>
                        <span className="text-slate-200 font-mono">{selectedItem.type}</span>
                    </div>
                </div>
                
                {selectedItem.detailedDescription && (
                    <div>
                         <h4 className="text-sm font-medium text-slate-300 mb-2">Detailed Description</h4>
                         <div className="p-4 bg-slate-950 rounded-lg border border-slate-800 text-slate-400 text-sm leading-relaxed">
                            {selectedItem.detailedDescription}
                         </div>
                    </div>
                )}

                {selectedItem.alphabetJson && (
                     <div>
                        <h4 className="text-sm font-medium text-slate-300 mb-2">Alphabet</h4>
                        <div className="flex flex-wrap gap-2">
                            {parseAlphabet(selectedItem.alphabetJson).map((char: string, i: number) => (
                                <span key={i} className="px-2 py-1 bg-slate-950 border border-slate-800 rounded text-sm font-mono text-brand-400">
                                    {char}
                                </span>
                            ))}
                        </div>
                    </div>
                )}
                
                <div>
                    <div className="flex justify-between items-center mb-2">
                        <h4 className="text-sm font-medium text-slate-300">Source JSON</h4>
                        <span className="text-xs text-slate-500 font-mono">Read Only</span>
                    </div>
                    <div className="relative">
                        <pre className="bg-[#0d1117] p-4 rounded-lg border border-slate-800 text-xs font-mono text-slate-300 overflow-auto max-h-[300px] custom-scrollbar">
                            {JSON.stringify(selectedItem, null, 2)}
                        </pre>
                    </div>
                </div>
            </div>
        ) : null}
      </Modal>

      <ConfirmDialog 
        isOpen={confirmOpen}
        title="Delete Workshop Item?"
        message={`Are you sure you want to delete item #${itemToDelete}? This action cannot be undone.`}
        confirmText="Delete Item"
        isDestructive={true}
        onCancel={() => setConfirmOpen(false)}
        onConfirm={handleExecuteDelete}
      />
    </div>
  );
};
