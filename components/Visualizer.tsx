
import React, { useEffect, useRef, useState, useCallback } from 'react';
import { X, Play, Pause, SkipForward, RotateCcw, FastForward, ZoomIn, ZoomOut, Bug } from 'lucide-react';
import { TuringSimulator, SimulationState, Tape } from '../services/simulation';
import { WorkshopItemDetail } from '../types';

interface VisualizerProps {
  isOpen: boolean;
  onClose: () => void;
  level: WorkshopItemDetail;
  machine: WorkshopItemDetail;
  initialInput?: string;
}

export const Visualizer: React.FC<VisualizerProps> = ({ isOpen, onClose, level, machine, initialInput = "" }) => {
  const [simulator, setSimulator] = useState<TuringSimulator | null>(null);
  const [simState, setSimState] = useState<SimulationState | null>(null);
  const [input, setInput] = useState(initialInput);
  const [isPlaying, setIsPlaying] = useState(false);
  const [speed, setSpeed] = useState(500); // ms per step
  const [scale, setScale] = useState(1);
  
  // Breakpoints state
  const [breakpoints, setBreakpoints] = useState<Set<number>>(new Set());

  // Canvas/Graph State
  const [viewBox, setViewBox] = useState("0 0 800 600");

  // Setup Simulator
  useEffect(() => {
    if (machine) {
      const sim = new TuringSimulator(machine);
      setSimulator(sim);
      // Auto-center logic
      if (sim.nodes.length > 0) {
          const xs = sim.nodes.map(n => n.x);
          const ys = sim.nodes.map(n => n.y);
          const minX = Math.min(...xs);
          const maxX = Math.max(...xs);
          const minY = Math.min(...ys);
          const maxY = Math.max(...ys);
          const padding = 150;
          const width = Math.max(800, maxX - minX + padding * 2);
          const height = Math.max(600, maxY - minY + padding * 2);
          const cx = minX - padding;
          const cy = minY - padding;
          setViewBox(`${cx} ${cy} ${width} ${height}`);
      }
      reset(sim);
    }
  }, [machine]);

  // Keyboard Shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
        if (!isOpen) return;
        
        // Ignore if typing in input
        if ((e.target as HTMLElement).tagName === 'INPUT') return;

        switch(e.code) {
            case 'Space':
                e.preventDefault();
                if (simState?.status === 'running') setIsPlaying(p => !p);
                break;
            case 'ArrowRight':
                e.preventDefault();
                setIsPlaying(false);
                step();
                break;
            case 'KeyR':
                if (e.ctrlKey || e.metaKey) return; // Don't block refresh
                reset();
                break;
        }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, simState]);

  // Reset Logic
  const reset = (sim: TuringSimulator = simulator!) => {
    if (!sim) return;
    setIsPlaying(false);
    setSimState(sim.initSession(input, level.twoTapes || false));
  };

  // Watch input changes to auto-reset
  useEffect(() => {
    if (simulator) reset(simulator);
  }, [input]);

  // Step Logic
  const step = useCallback(() => {
    if (simulator && simState) {
      setSimState(prev => {
          if (!prev) return null;
          return simulator.step(prev, level.twoTapes || false)
      });
    }
  }, [simulator, simState, level]);

  // Playback Loop with Breakpoint check
  useEffect(() => {
    let interval: any;
    
    if (isPlaying && simState?.status === 'running') {
      
      // Check if we hit a breakpoint (and ensure we don't get stuck on it forever by checking previous state effectively via the loop)
      // Actually, simple logic: If current node IS a breakpoint, we pause.
      // But we need to allow moving OFF it. 
      // So we check inside the interval: perform step -> check result.
      
      interval = setInterval(() => {
        setSimState(current => {
            if (!current || !simulator) return null;
            
            const next = simulator.step(current, level.twoTapes || false);
            
            // Breakpoint Hit Logic
            if (breakpoints.has(next.currentNodeId) && next.currentNodeId !== current.currentNodeId) {
                setIsPlaying(false); // Pause
            }
            
            return next;
        });
      }, speed);
    } else if (simState?.status !== 'running') {
      setIsPlaying(false);
    }
    return () => clearInterval(interval);
  }, [isPlaying, simState, speed, breakpoints, simulator, level]);


  const toggleBreakpoint = (id: number) => {
      setBreakpoints(prev => {
          const newSet = new Set(prev);
          if (newSet.has(id)) newSet.delete(id);
          else newSet.add(id);
          return newSet;
      });
  };

  if (!isOpen || !simulator || !simState) return null;

  // --- Rendering Helpers ---

  const renderTape = (tape: Tape, label: string) => {
    // Determine window of cells to show (centered on head)
    const windowSize = 21; // odd number
    const half = Math.floor(windowSize / 2);
    const startIdx = tape.head - half;
    const cells = [];
    
    for (let i = 0; i < windowSize; i++) {
      const idx = startIdx + i;
      const char = tape.data[idx] || '_';
      const isHead = idx === tape.head;
      
      cells.push(
        <div key={idx} className={`
          flex-shrink-0 w-10 h-12 flex items-center justify-center border-r border-slate-700 font-mono text-lg
          ${isHead ? 'bg-brand-500/20 text-brand-400 border-b-4 border-b-brand-500' : 'text-slate-300 bg-slate-900'}
        `}>
          {char}
        </div>
      );
    }

    return (
      <div className="flex flex-col gap-1">
        <span className="text-xs text-slate-500 font-bold uppercase tracking-wider ml-1">{label}</span>
        <div className="flex border border-slate-700 rounded-md overflow-hidden bg-slate-950 relative">
          {cells}
          <div className="absolute inset-0 pointer-events-none shadow-inner shadow-black/50"></div>
        </div>
      </div>
    );
  };

  return (
    <div className="fixed inset-0 z-[100] bg-slate-950 flex flex-col animate-in fade-in duration-300">
      {/* Top Bar */}
      <div className="h-16 border-b border-slate-800 bg-slate-900/50 px-6 flex items-center justify-between">
        <div className="flex items-center gap-4">
           <div>
              <h2 className="text-lg font-bold text-slate-100">{machine.name}</h2>
              <p className="text-xs text-slate-500">Visualizing on level: {level.name}</p>
           </div>
           <div className="h-8 w-px bg-slate-800 mx-2"></div>
           <div className="flex items-center gap-4 text-sm">
              <div className="flex flex-col">
                 <span className="text-[10px] text-slate-500 uppercase">Status</span>
                 <span className={`font-bold ${
                    simState.status === 'running' ? 'text-blue-400' :
                    simState.status === 'accepted' ? 'text-emerald-400' :
                    simState.status === 'rejected' ? 'text-red-400' : 'text-slate-400'
                 }`}>
                    {simState.status.toUpperCase()}
                 </span>
              </div>
              <div className="flex flex-col">
                 <span className="text-[10px] text-slate-500 uppercase">Steps</span>
                 <span className="text-slate-200 font-mono">{simState.steps}</span>
              </div>
           </div>
        </div>

        <div className="flex items-center gap-4">
           <div className="flex items-center gap-2 text-slate-500 text-xs mr-4">
                <span className="flex items-center gap-1 bg-slate-900 px-2 py-1 rounded border border-slate-800">
                    <span className="font-bold">Space</span> Play/Pause
                </span>
                <span className="flex items-center gap-1 bg-slate-900 px-2 py-1 rounded border border-slate-800">
                    <span className="font-bold">→</span> Step
                </span>
           </div>

          <div className="flex items-center gap-2 bg-slate-900 p-1 rounded-lg border border-slate-800">
              <span className="text-xs text-slate-500 px-2">Input:</span>
              <input 
                value={input}
                onChange={(e) => setInput(e.target.value)}
                className="bg-transparent border-none outline-none text-sm text-slate-200 w-32 font-mono placeholder-slate-700"
                placeholder="Enter tape..."
                disabled={isPlaying}
              />
              <button onClick={() => reset()} className="p-1.5 hover:bg-slate-800 rounded text-slate-400 hover:text-white" title="Reset">
                  <RotateCcw size={14} />
              </button>
          </div>
          <button onClick={onClose} className="p-2 hover:bg-red-500/10 hover:text-red-400 rounded-lg transition-colors text-slate-400">
            <X size={24} />
          </button>
        </div>
      </div>

      {/* Middle: Graph Area */}
      <div className="flex-1 bg-[#020617] relative overflow-hidden select-none cursor-grab active:cursor-grabbing">
        <div className="absolute top-4 right-4 flex flex-col gap-2 z-10">
            <button onClick={() => setScale(s => Math.min(s + 0.1, 3))} className="p-2 bg-slate-800/80 rounded-lg text-slate-400 hover:text-white"><ZoomIn size={20} /></button>
            <button onClick={() => setScale(s => Math.max(s - 0.1, 0.2))} className="p-2 bg-slate-800/80 rounded-lg text-slate-400 hover:text-white"><ZoomOut size={20} /></button>
        </div>

        <div className="absolute top-4 left-4 z-10 pointer-events-none">
            <div className="bg-slate-900/80 backdrop-blur px-3 py-2 rounded-lg border border-slate-800 text-xs text-slate-400 flex items-center gap-2">
                <Bug size={14} />
                Click nodes to set breakpoints
            </div>
        </div>

        <svg width="100%" height="100%" viewBox={viewBox} style={{transform: `scale(${scale})`, transformOrigin: 'center'}}>
          <defs>
             <marker id="arrowhead" markerWidth="10" markerHeight="7" refX="28" refY="3.5" orient="auto">
                <polygon points="0 0, 10 3.5, 0 7" fill="#64748b" />
             </marker>
             <marker id="arrowhead-active" markerWidth="10" markerHeight="7" refX="28" refY="3.5" orient="auto">
                <polygon points="0 0, 10 3.5, 0 7" fill="#38bdf8" />
             </marker>
          </defs>

          {/* Connections */}
          {simulator.connections.map((conn: any, idx: number) => {
            const start = simulator.nodes.find(n => n.id === conn.start);
            const end = simulator.nodes.find(n => n.id === conn.end);
            if (!start || !end) return null;
            
            const isActive = simState.lastTransitionId === idx;
            const color = isActive ? '#38bdf8' : '#334155';
            const width = isActive ? 3 : 1.5;

            let path = "";
            let labelX = 0;
            let labelY = 0;

            if (start.id === end.id) {
                // Loop
                path = `M ${start.x} ${start.y - 25} C ${start.x - 50} ${start.y - 100}, ${start.x + 50} ${start.y - 100}, ${start.x} ${start.y - 25}`;
                labelX = start.x;
                labelY = start.y - 105; 
            } else {
                path = `M ${start.x} ${start.y} L ${end.x} ${end.y}`;
                labelX = (start.x + end.x) / 2;
                labelY = (start.y + end.y) / 2;
            }

            // Annotation Text Logic
            let labelText = "";
            const read1 = conn.read ? JSON.stringify(conn.read).replace(/"/g, '').replace(/,/g, '') : '_';
            const write1 = conn.write || '_';
            const move1 = conn.move || 'S';
            
            if (level.twoTapes) {
                const read2 = conn.read2 ? JSON.stringify(conn.read2).replace(/"/g, '').replace(/,/g, '') : '_';
                const write2 = conn.write2 || '_';
                const move2 = conn.move2 || 'S';
                labelText = `[${read1}]/[${read2}] → ${write1}/${write2}, ${move1}${move2}`;
            } else {
                labelText = `${read1} → ${write1}, ${move1}`;
            }

            return (
                <g key={idx}>
                    <path 
                        d={path} 
                        fill="none" 
                        stroke={color} 
                        strokeWidth={width} 
                        markerEnd={`url(#${isActive ? 'arrowhead-active' : 'arrowhead'})`}
                    />
                    {/* Label Background */}
                    <rect 
                        x={labelX - (labelText.length * 3)} 
                        y={labelY - 10} 
                        width={labelText.length * 6 + 10} 
                        height="16" 
                        fill="#020617" 
                        rx="4"
                        opacity="0.8"
                    />
                    {/* Label Text */}
                    <text
                        x={labelX}
                        y={labelY}
                        dy="2"
                        textAnchor="middle"
                        fill={isActive ? '#38bdf8' : '#94a3b8'}
                        fontSize="9"
                        fontFamily="monospace"
                        fontWeight="bold"
                    >
                        {labelText}
                    </text>
                </g>
            );
          })}

          {/* Nodes */}
          {simulator.nodes.map((node: any) => {
             const isCurrent = simState.currentNodeId === node.id;
             const isBreakpoint = breakpoints.has(node.id);

             return (
                <g 
                    key={node.id} 
                    transform={`translate(${node.x}, ${node.y})`}
                    onClick={() => toggleBreakpoint(node.id)}
                    className="cursor-pointer"
                >
                    {/* Breakpoint Glow/Indicator */}
                    {isBreakpoint && (
                         <circle r="29" fill="none" stroke="#ef4444" strokeWidth="2" opacity="0.6" className="animate-pulse" />
                    )}

                    <circle 
                        r="25" 
                        fill={isCurrent ? '#0ea5e9' : '#1e293b'} 
                        stroke={isCurrent ? '#38bdf8' : isBreakpoint ? '#ef4444' : '#475569'}
                        strokeWidth={isCurrent ? 3 : isBreakpoint ? 2 : 2}
                        className="transition-all duration-200 hover:stroke-brand-400"
                    />
                    {node.is_end && <circle r="20" fill="none" stroke={isCurrent ? '#38bdf8' : '#475569'} strokeWidth="1" />}
                    
                    {/* Breakpoint dot */}
                    {isBreakpoint && (
                         <circle cx="18" cy="-18" r="6" fill="#ef4444" stroke="#020617" strokeWidth="2" />
                    )}

                    <text 
                        textAnchor="middle" 
                        dy=".3em" 
                        fill={isCurrent ? '#fff' : '#94a3b8'} 
                        className="font-mono font-bold text-sm pointer-events-none"
                    >
                        {node.id}
                    </text>
                </g>
             )
          })}
        </svg>
      </div>

      {/* Bottom: Controls & Tapes */}
      <div className="h-[280px] bg-slate-900 border-t border-slate-800 flex flex-col">
         
         {/* Tapes Area */}
         <div className="flex-1 p-6 flex flex-col justify-center gap-6 overflow-hidden bg-black/20">
            {renderTape(simState.tape1, "Tape 1")}
            {level.twoTapes && renderTape(simState.tape2, "Tape 2")}
         </div>

         {/* Control Bar */}
         <div className="h-16 bg-slate-950 border-t border-slate-800 px-6 flex items-center justify-center gap-8">
            <button onClick={() => reset()} className="p-2 rounded-full hover:bg-slate-800 text-slate-400 hover:text-white transition-colors" title="Reset (R)">
                <RotateCcw size={20} />
            </button>
            
            <div className="flex items-center gap-4">
                <button 
                    onClick={() => { setIsPlaying(false); step(); }} 
                    className="p-3 rounded-full bg-slate-800 hover:bg-slate-700 text-white transition-all border border-slate-700"
                    disabled={simState.status !== 'running'}
                    title="Step Forward (Right Arrow)"
                >
                    <SkipForward size={20} />
                </button>
                
                <button 
                    onClick={() => setIsPlaying(!isPlaying)} 
                    className={`p-4 rounded-full text-white shadow-lg transition-all ${
                        isPlaying ? 'bg-amber-600 hover:bg-amber-500 shadow-amber-500/30' : 'bg-brand-600 hover:bg-brand-500 shadow-brand-500/30'
                    }`}
                    disabled={simState.status !== 'running' && !isPlaying}
                    title="Play/Pause (Space)"
                >
                    {isPlaying ? <Pause size={24} fill="white" /> : <Play size={24} fill="white" className="ml-1" />}
                </button>
            </div>

            {/* Speed Control */}
            <div className="flex items-center gap-3 w-48">
                <span className="text-slate-500 text-xs uppercase font-bold">Speed</span>
                <input 
                    type="range" 
                    min="50" 
                    max="1000" 
                    step="50"
                    className="w-full h-1 bg-slate-800 rounded-lg appearance-none cursor-pointer accent-brand-500"
                    value={1050 - speed} // Invert so right is faster
                    onChange={(e) => setSpeed(1050 - parseInt(e.target.value))}
                />
                <FastForward size={16} className="text-slate-500" />
            </div>
         </div>
      </div>
    </div>
  );
};
