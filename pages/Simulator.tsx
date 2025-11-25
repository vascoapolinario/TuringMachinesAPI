
import React, { useState, useEffect } from 'react';
import { useSearchParams } from 'react-router-dom';
import { api } from '../services/api';
import { TuringSimulator, TestCaseResult } from '../services/simulation';
import { WorkshopItem, WorkshopItemDetail } from '../types';
import { Visualizer } from '../components/Visualizer';
import { 
  Play, CheckCircle, XCircle, ArrowRight, Settings, RotateCcw, 
  Eye, Search, Cpu, Layers, Terminal, Activity, Zap, Bug, ChevronRight
} from 'lucide-react';

export const Simulator: React.FC = () => {
  const [searchParams] = useSearchParams();

  // Selection Lists
  const [levels, setLevels] = useState<WorkshopItem[]>([]);
  const [machines, setMachines] = useState<WorkshopItem[]>([]);
  
  // Search Filter State
  const [levelSearch, setLevelSearch] = useState('');
  const [machineSearch, setMachineSearch] = useState('');

  // Selected IDs
  const [selectedLevelId, setSelectedLevelId] = useState<number | null>(null);
  const [selectedMachineId, setSelectedMachineId] = useState<number | null>(null);

  // Full Data
  const [levelData, setLevelData] = useState<WorkshopItemDetail | null>(null);
  const [machineData, setMachineData] = useState<WorkshopItemDetail | null>(null);

  const [loading, setLoading] = useState(true);
  const [simulating, setSimulating] = useState(false);
  const [results, setResults] = useState<TestCaseResult[] | null>(null);
  const [error, setError] = useState('');

  // Visualizer State
  const [isVisualizerOpen, setIsVisualizerOpen] = useState(false);
  const [visualizerInput, setVisualizerInput] = useState("");

  // Initial Load
  useEffect(() => {
    const fetchData = async () => {
      try {
        const items = await api.workshop.getAll();
        setLevels(items.filter(i => i.type === 'Level'));
        setMachines(items.filter(i => i.type === 'Machine'));

        const queryLevelId = searchParams.get('levelId');
        const queryMachineId = searchParams.get('machineId');

        if (queryLevelId) setSelectedLevelId(parseInt(queryLevelId));
        if (queryMachineId) setSelectedMachineId(parseInt(queryMachineId));

      } catch (e) {
        setError("Failed to load workshop items.");
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [searchParams]);

  // Fetch Full Data when selection changes
  useEffect(() => {
    if (!selectedLevelId) {
        setLevelData(null); 
        return;
    }
    const fetchLevel = async () => {
      try {
        const data = await api.workshop.get(selectedLevelId);
        setLevelData(data);
        setResults(null); 
      } catch(e) { console.error(e); }
    };
    fetchLevel();
  }, [selectedLevelId]);

  useEffect(() => {
    if (!selectedMachineId) {
        setMachineData(null);
        return;
    }
    const fetchMachine = async () => {
      try {
        const data = await api.workshop.get(selectedMachineId);
        setMachineData(data);
        setResults(null);
      } catch(e) { console.error(e); }
    };
    fetchMachine();
  }, [selectedMachineId]);


  const handleRun = () => {
    if (!levelData || !machineData) return;
    setSimulating(true);
    setResults(null);

    setTimeout(() => {
      try {
        const sim = new TuringSimulator(machineData);
        const res = sim.runLevelTests(levelData);
        setResults(res);
      } catch (e: any) {
        setError("Simulation Error: " + e.message);
      } finally {
        setSimulating(false);
      }
    }, 400); // Small delay for effect
  };

  const handleVisualize = (input?: string) => {
    if (!input) {
        try {
            if (levelData?.transformTestsJson) {
                const tests = JSON.parse(levelData.transformTestsJson);
                if (tests.length > 0) input = tests[0].input;
            } else if (levelData?.correctExamplesJson) {
                const tests = JSON.parse(levelData.correctExamplesJson);
                if (tests.length > 0) input = tests[0];
            }
        } catch (e) {}
    }
    setVisualizerInput(input || "");
    setIsVisualizerOpen(true);
  };

  const overallPass = results && results.length > 0 && results.every(r => r.passed);
  const filteredLevels = levels.filter(l => l.name.toLowerCase().includes(levelSearch.toLowerCase()));
  const filteredMachines = machines.filter(m => m.name.toLowerCase().includes(machineSearch.toLowerCase()));

  return (
    <div className="h-full flex flex-col md:flex-row bg-slate-950 overflow-hidden">
      
      {/* --- LEFT PANEL: CONTROL CENTER --- */}
      <div className="w-full md:w-[400px] lg:w-[450px] bg-slate-900 border-r border-slate-800 flex flex-col shrink-0 z-10 shadow-2xl">
        <div className="p-6 border-b border-slate-800">
           <h1 className="text-xl font-bold text-slate-100 flex items-center gap-2">
              <Activity className="text-brand-500" /> Diagnostic Facility
           </h1>
           <p className="text-xs text-slate-500 mt-1">Configure environment and test unit.</p>
        </div>

        <div className="flex-1 overflow-y-auto custom-scrollbar p-6 space-y-6">
            
            {/* LEVEL SELECTOR */}
            <div className="space-y-3">
                <div className="flex items-center justify-between">
                    <label className="text-xs font-bold text-slate-500 uppercase tracking-wider flex items-center gap-2">
                        <Layers size={14} /> Environment (Level)
                    </label>
                    {levelData && (
                        <span className={`text-[10px] px-2 py-0.5 rounded border ${levelData.twoTapes ? 'bg-purple-500/10 text-purple-400 border-purple-500/20' : 'bg-slate-800 text-slate-400 border-slate-700'}`}>
                            {levelData.twoTapes ? '2 Tapes' : '1 Tape'}
                        </span>
                    )}
                </div>
                
                <div className="relative group">
                     <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors" size={14} />
                     <input 
                        type="text"
                        placeholder="Search levels..."
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg py-2.5 pl-9 pr-4 text-slate-200 text-xs focus:outline-none focus:border-brand-500/50 focus:ring-1 focus:ring-brand-500/50 transition-all"
                        value={levelSearch}
                        onChange={(e) => setLevelSearch(e.target.value)}
                    />
                </div>

                <div className="h-48 overflow-y-auto custom-scrollbar bg-slate-950/50 rounded-xl border border-slate-800/50 p-1 space-y-1">
                    {filteredLevels.map(l => (
                        <button
                            key={l.id}
                            onClick={() => setSelectedLevelId(l.id)}
                            className={`w-full text-left px-3 py-2.5 rounded-lg text-sm transition-all flex items-center gap-3 border ${
                                selectedLevelId === l.id 
                                ? 'bg-brand-500/10 border-brand-500/30 text-brand-100 shadow-[0_0_10px_rgba(14,165,233,0.1)]' 
                                : 'bg-transparent border-transparent hover:bg-slate-800 text-slate-400'
                            }`}
                        >
                            <div className={`p-1.5 rounded-md ${selectedLevelId === l.id ? 'bg-brand-500 text-white' : 'bg-slate-800 text-slate-500'}`}>
                                <Layers size={14} />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="font-medium truncate text-xs">{l.name}</div>
                            </div>
                            {selectedLevelId === l.id && <div className="w-1.5 h-1.5 rounded-full bg-brand-400 shadow-glow"></div>}
                        </button>
                    ))}
                </div>
            </div>

            {/* SEPARATOR */}
            <div className="h-px bg-slate-800 w-full"></div>

            {/* MACHINE SELECTOR */}
            <div className="space-y-3">
                <label className="text-xs font-bold text-slate-500 uppercase tracking-wider flex items-center gap-2">
                    <Cpu size={14} /> Test Unit (Machine)
                </label>
                
                <div className="relative group">
                     <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-cyan-400 transition-colors" size={14} />
                     <input 
                        type="text"
                        placeholder="Search machines..."
                        className="w-full bg-slate-950 border border-slate-800 rounded-lg py-2.5 pl-9 pr-4 text-slate-200 text-xs focus:outline-none focus:border-cyan-500/50 focus:ring-1 focus:ring-cyan-500/50 transition-all"
                        value={machineSearch}
                        onChange={(e) => setMachineSearch(e.target.value)}
                    />
                </div>

                <div className="h-48 overflow-y-auto custom-scrollbar bg-slate-950/50 rounded-xl border border-slate-800/50 p-1 space-y-1">
                    {filteredMachines.map(m => (
                        <button
                            key={m.id}
                            onClick={() => setSelectedMachineId(m.id)}
                            className={`w-full text-left px-3 py-2.5 rounded-lg text-sm transition-all flex items-center gap-3 border ${
                                selectedMachineId === m.id 
                                ? 'bg-cyan-500/10 border-cyan-500/30 text-cyan-100 shadow-[0_0_10px_rgba(6,182,212,0.1)]' 
                                : 'bg-transparent border-transparent hover:bg-slate-800 text-slate-400'
                            }`}
                        >
                            <div className={`p-1.5 rounded-md ${selectedMachineId === m.id ? 'bg-cyan-500 text-white' : 'bg-slate-800 text-slate-500'}`}>
                                <Cpu size={14} />
                            </div>
                            <div className="flex-1 min-w-0">
                                <div className="font-medium truncate text-xs">{m.name}</div>
                                <div className="text-[10px] text-slate-500 truncate">by {m.author}</div>
                            </div>
                            {selectedMachineId === m.id && <div className="w-1.5 h-1.5 rounded-full bg-cyan-400 shadow-glow"></div>}
                        </button>
                    ))}
                </div>
            </div>
        </div>

        {/* Action Footer */}
        <div className="p-6 border-t border-slate-800 bg-slate-900/50 space-y-3">
             <button
                onClick={() => handleVisualize()}
                disabled={!selectedLevelId || !selectedMachineId}
                className="w-full py-3 bg-slate-800 hover:bg-slate-700 disabled:opacity-50 disabled:cursor-not-allowed text-slate-200 hover:text-white rounded-xl border border-slate-700 font-bold text-sm transition-all flex items-center justify-center gap-2 group"
              >
                 <Eye size={16} className="text-brand-400 group-hover:text-brand-300" />
                 Open Visual Debugger
              </button>

             <button
                onClick={handleRun}
                disabled={!selectedLevelId || !selectedMachineId || simulating}
                className="w-full py-4 bg-gradient-to-r from-brand-600 to-cyan-600 hover:from-brand-500 hover:to-cyan-500 text-white font-bold rounded-xl shadow-lg shadow-brand-500/20 disabled:opacity-50 disabled:cursor-not-allowed transition-all flex items-center justify-center gap-2 transform active:scale-[0.98]"
              >
                {simulating ? (
                    <>
                        <RotateCcw className="animate-spin" size={20} />
                        Running Diagnostics...
                    </>
                ) : (
                    <>
                        <Play size={20} fill="currentColor" />
                        Run Validation Suite
                    </>
                )}
              </button>
              
              {error && (
                  <div className="text-xs text-red-400 text-center font-medium bg-red-500/10 py-2 rounded-lg border border-red-500/20">
                      {error}
                  </div>
              )}
        </div>
      </div>

      {/* --- RIGHT PANEL: OUTPUT TERMINAL --- */}
      <div className="flex-1 bg-slate-950 flex flex-col min-w-0 relative">
        {/* Background Grid */}
        <div className="absolute inset-0 opacity-20 pointer-events-none" style={{ backgroundImage: 'radial-gradient(circle, #334155 1px, transparent 1px)', backgroundSize: '24px 24px' }}></div>
        
        {/* Status Bar */}
        <div className="h-12 border-b border-slate-800 bg-slate-900/50 backdrop-blur flex items-center justify-between px-6 shrink-0 relative z-10">
            <div className="flex items-center gap-3">
                <Terminal size={16} className="text-slate-500" />
                <span className="text-xs font-mono text-slate-400 uppercase tracking-widest">System Output</span>
            </div>
            {results && (
                 <div className="flex items-center gap-2 text-xs font-mono">
                     <span className="text-slate-500">Execution Time:</span>
                     <span className="text-brand-400">~12ms</span>
                 </div>
            )}
        </div>

        <div className="flex-1 overflow-y-auto custom-scrollbar p-8 relative z-10">
            {!results ? (
                <div className="h-full flex flex-col items-center justify-center text-slate-600 space-y-6">
                    <div className="w-32 h-32 rounded-full bg-slate-900 border border-slate-800 flex items-center justify-center shadow-2xl relative overflow-hidden group">
                        <div className="absolute inset-0 bg-[conic-gradient(from_90deg_at_50%_50%,#0f172a_50%,#334155_100%)] animate-[spin_4s_linear_infinite] opacity-0 group-hover:opacity-100 transition-opacity"></div>
                        <div className="absolute inset-1 bg-slate-900 rounded-full z-10 flex items-center justify-center">
                            <Settings size={48} className="opacity-50" />
                        </div>
                    </div>
                    <div className="text-center">
                        <h2 className="text-xl font-bold text-slate-300 tracking-tight">System Standby</h2>
                        <p className="text-sm mt-2 max-w-xs mx-auto">Select a target configuration from the control panel to initialize the testing sequence.</p>
                    </div>
                </div>
            ) : (
                <div className="max-w-5xl mx-auto space-y-8 animate-in slide-in-from-bottom-4 duration-500">
                    
                    {/* Header Summary */}
                    <div className={`p-6 rounded-2xl border flex items-center justify-between ${
                        overallPass 
                        ? 'bg-emerald-500/5 border-emerald-500/20 shadow-[0_0_30px_rgba(16,185,129,0.1)]' 
                        : 'bg-red-500/5 border-red-500/20 shadow-[0_0_30px_rgba(239,68,68,0.1)]'
                    }`}>
                        <div className="flex items-center gap-6">
                            <div className={`w-16 h-16 rounded-2xl flex items-center justify-center shadow-lg ${
                                overallPass ? 'bg-emerald-500 text-slate-900' : 'bg-red-500 text-slate-900'
                            }`}>
                                {overallPass ? <CheckCircle size={32} /> : <XCircle size={32} />}
                            </div>
                            <div>
                                <h2 className={`text-2xl font-bold tracking-tight ${overallPass ? 'text-white' : 'text-red-100'}`}>
                                    {overallPass ? 'Systems Nominal' : 'Critical Failure'}
                                </h2>
                                <p className={`text-sm font-medium ${overallPass ? 'text-emerald-400' : 'text-red-400'}`}>
                                    {overallPass ? 'All validation checks passed successfully.' : 'Logic errors detected in output stream.'}
                                </p>
                            </div>
                        </div>
                        <div className="text-right hidden sm:block">
                            <div className="text-3xl font-mono font-bold text-white">
                                {results.filter(r => r.passed).length}<span className="text-slate-600 text-xl mx-2">/</span>{results.length}
                            </div>
                            <div className="text-xs uppercase tracking-widest text-slate-500 font-bold mt-1">Tests Passed</div>
                        </div>
                    </div>

                    {/* Test List */}
                    <div className="grid grid-cols-1 gap-4">
                        {results.map((res, idx) => (
                            <div key={idx} className={`group relative bg-slate-900 border rounded-xl overflow-hidden transition-all duration-300 ${
                                res.passed 
                                ? 'border-slate-800 hover:border-emerald-500/30' 
                                : 'border-red-900/50 hover:border-red-500/50'
                            }`}>
                                {/* Status Strip */}
                                <div className={`absolute left-0 top-0 bottom-0 w-1 ${res.passed ? 'bg-emerald-500' : 'bg-red-500'}`}></div>
                                
                                <div className="p-5 pl-7 flex flex-col md:flex-row gap-6 items-start md:items-center">
                                    {/* ID & Status */}
                                    <div className="flex items-center gap-4 min-w-[120px]">
                                        <div className="flex flex-col">
                                            <span className="text-[10px] uppercase text-slate-500 font-bold tracking-wider">Test Case</span>
                                            <span className="font-mono text-lg text-slate-300 font-bold">#{String(idx + 1).padStart(2, '0')}</span>
                                        </div>
                                        <div className={`px-2.5 py-1 rounded text-xs font-bold uppercase tracking-wider ${
                                            res.passed ? 'bg-emerald-500/10 text-emerald-400' : 'bg-red-500/10 text-red-400'
                                        }`}>
                                            {res.passed ? 'PASS' : 'FAIL'}
                                        </div>
                                    </div>

                                    {/* Data Visualization */}
                                    <div className="flex-1 grid grid-cols-1 md:grid-cols-2 gap-6 w-full">
                                        <div className="space-y-1.5">
                                            <div className="flex items-center gap-2 text-xs text-slate-500 uppercase font-bold tracking-wider">
                                                <Zap size={12} /> Input
                                            </div>
                                            <div className="font-mono text-sm bg-slate-950 border border-slate-800 rounded px-3 py-2 text-slate-300 break-all shadow-inner">
                                                {res.input || <span className="text-slate-600 italic">Empty Tape</span>}
                                            </div>
                                        </div>

                                        <div className="space-y-1.5">
                                            <div className="flex items-center gap-2 text-xs text-slate-500 uppercase font-bold tracking-wider">
                                                <Terminal size={12} /> Output {res.passed ? '' : '(Expected vs Actual)'}
                                            </div>
                                            
                                            {typeof res.expected === 'string' ? (
                                                // Transform Mode Display
                                                <div className="flex items-center gap-2">
                                                    {!res.passed && (
                                                        <>
                                                            <div className="font-mono text-sm bg-slate-950/50 border border-slate-800 rounded px-3 py-2 text-slate-500 line-through opacity-70 break-all flex-1">
                                                                {res.expected}
                                                            </div>
                                                            <ChevronRight size={16} className="text-slate-600" />
                                                        </>
                                                    )}
                                                    <div className={`font-mono text-sm border rounded px-3 py-2 break-all flex-1 shadow-inner ${
                                                        res.passed 
                                                        ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-300' 
                                                        : 'bg-red-500/5 border-red-500/20 text-red-300'
                                                    }`}>
                                                        {res.actual}
                                                    </div>
                                                </div>
                                            ) : (
                                                // Accept Mode Display
                                                <div className="flex items-center gap-2">
                                                     <div className={`font-mono text-sm border rounded px-3 py-2 flex-1 shadow-inner font-bold ${
                                                        res.passed 
                                                        ? 'bg-emerald-500/5 border-emerald-500/20 text-emerald-300' 
                                                        : 'bg-red-500/5 border-red-500/20 text-red-300'
                                                    }`}>
                                                        {res.actual ? 'ACCEPTED' : 'REJECTED'}
                                                    </div>
                                                </div>
                                            )}
                                        </div>
                                    </div>

                                    {/* Action */}
                                    <button 
                                        onClick={() => handleVisualize(res.input)}
                                        className="p-3 bg-slate-950 hover:bg-brand-600 hover:text-white text-slate-500 rounded-lg border border-slate-800 transition-colors shadow-sm self-end md:self-center"
                                        title="Debug in Visualizer"
                                    >
                                        <Bug size={18} />
                                    </button>
                                </div>
                                
                                {/* Logs Footer */}
                                {res.logs.length > 0 && (
                                    <div className="bg-slate-950/50 border-t border-slate-800/50 px-7 py-2 text-[10px] font-mono text-slate-500 flex gap-4">
                                        {res.logs.map((log, i) => <span key={i}>{log}</span>)}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
      </div>

      {levelData && machineData && (
        <Visualizer 
            isOpen={isVisualizerOpen} 
            onClose={() => setIsVisualizerOpen(false)}
            level={levelData}
            machine={machineData}
            initialInput={visualizerInput}
        />
      )}
    </div>
  );
};
