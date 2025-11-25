
import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { 
  Play, Pause, Plus, Trash2, Save, Download, Upload, 
  ZoomIn, ZoomOut, Settings, RotateCcw, MousePointer2, 
  Move, ArrowRight, Disc, FileCode, Check
} from 'lucide-react';
import { Card } from '../components/Card';
import { MachineNode, MachineConnection } from '../types';
import { TuringSimulator, SimulationState, Tape } from '../services/simulation';
import { Modal } from '../components/Modal';

export const Game: React.FC = () => {
  // --- Editor State ---
  const [nodes, setNodes] = useState<MachineNode[]>([
    { id: 0, x: 100, y: 300, is_start: true, is_end: false },
    { id: 1, x: 400, y: 300, is_start: false, is_end: false }
  ]);
  const [connections, setConnections] = useState<MachineConnection[]>([]);
  const [twoTapes, setTwoTapes] = useState(false);
  
  // --- UI State ---
  const [scale, setScale] = useState(1);
  const [offset, setOffset] = useState({ x: 0, y: 0 });
  const [selectedNodeId, setSelectedNodeId] = useState<number | null>(null);
  const [selectedConnIndex, setSelectedConnIndex] = useState<number | null>(null);
  
  // Interaction State
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 });
  const [isPanning, setIsPanning] = useState(false);
  const [panStart, setPanStart] = useState({ x: 0, y: 0 });
  
  // Connection Creation State
  const [isConnecting, setIsConnecting] = useState(false);
  const [connectStartId, setConnectStartId] = useState<number | null>(null);
  const [mousePos, setMousePos] = useState({ x: 0, y: 0 });

  // Simulator State
  const [isSimulating, setIsSimulating] = useState(false);
  const [simState, setSimState] = useState<SimulationState | null>(null);
  const [simInput, setSimInput] = useState("");
  const [simulator, setSimulator] = useState<TuringSimulator | null>(null);

  // Code Export State
  const [showCodeModal, setShowCodeModal] = useState(false);
  const [generatedCode, setGeneratedCode] = useState("");
  const [copied, setCopied] = useState(false);

  const svgRef = useRef<SVGSVGElement>(null);

  // --- Helpers ---
  const getClientCoordinates = (e: React.MouseEvent | MouseEvent) => {
    if (svgRef.current) {
      const point = svgRef.current.createSVGPoint();
      point.x = e.clientX;
      point.y = e.clientY;
      const svgPoint = point.matrixTransform(svgRef.current.getScreenCTM()?.inverse());
      return { x: svgPoint.x, y: svgPoint.y };
    }
    return { x: 0, y: 0 };
  };

  // --- Interaction Handlers ---
  
  const handleMouseDown = (e: React.MouseEvent) => {
    // If clicking on background
    if (e.button === 0) { // Left click
       if (isConnecting) {
           setIsConnecting(false);
           setConnectStartId(null);
       } else {
           // Start panning
           setIsPanning(true);
           setPanStart({ x: e.clientX, y: e.clientY });
       }
       // Deselect
       setSelectedNodeId(null);
       setSelectedConnIndex(null);
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    const coords = getClientCoordinates(e);
    setMousePos(coords);

    if (isDragging && selectedNodeId !== null) {
      setNodes(prev => prev.map(n => 
        n.id === selectedNodeId ? { ...n, x: coords.x, y: coords.y } : n
      ));
    }

    if (isPanning) {
      const dx = e.clientX - panStart.x;
      const dy = e.clientY - panStart.y;
      setOffset(prev => ({ x: prev.x + dx, y: prev.y + dy }));
      setPanStart({ x: e.clientX, y: e.clientY });
    }
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    setIsPanning(false);
  };

  const handleNodeMouseDown = (e: React.MouseEvent, id: number) => {
    e.stopPropagation();
    if (e.button === 0) {
      if (isConnecting) {
          // Complete connection
          if (connectStartId !== null) {
              // Check if connection exists
              const exists = connections.findIndex(c => c.start === connectStartId && c.end === id);
              if (exists !== -1) {
                  setSelectedConnIndex(exists);
              } else {
                  // Create new
                  const newConn: MachineConnection = {
                      start: connectStartId,
                      end: id,
                      read: ['0'], write: '0', move: 'R',
                      read2: twoTapes ? ['_'] : undefined,
                      write2: twoTapes ? '_' : undefined,
                      move2: twoTapes ? 'S' : undefined
                  };
                  setConnections(prev => [...prev, newConn]);
                  setSelectedConnIndex(connections.length);
              }
              setIsConnecting(false);
              setConnectStartId(null);
          }
      } else {
          // Select and start drag
          setSelectedNodeId(id);
          setSelectedConnIndex(null);
          setIsDragging(true);
      }
    }
  };

  const startConnection = (e: React.MouseEvent, id: number) => {
      e.stopPropagation();
      setIsConnecting(true);
      setConnectStartId(id);
      setIsDragging(false);
  };

  const addNode = () => {
      const id = nodes.length > 0 ? Math.max(...nodes.map(n => n.id)) + 1 : 0;
      // Center in view
      const centerX = (-offset.x + (window.innerWidth / 2)) / scale;
      const centerY = (-offset.y + (window.innerHeight / 2)) / scale;
      
      setNodes(prev => [...prev, { id, x: 400, y: 300, is_start: false, is_end: false }]);
      setSelectedNodeId(id);
  };

  const deleteSelected = () => {
      if (selectedNodeId !== null) {
          setNodes(prev => prev.filter(n => n.id !== selectedNodeId));
          setConnections(prev => prev.filter(c => c.start !== selectedNodeId && c.end !== selectedNodeId));
          setSelectedNodeId(null);
      }
      if (selectedConnIndex !== null) {
          setConnections(prev => prev.filter((_, i) => i !== selectedConnIndex));
          setSelectedConnIndex(null);
      }
  };

  // --- Export Logic ---
  const generatePython = () => {
      const startNode = nodes.find(n => n.is_start);
      if (!startNode) return "# Error: No start node defined";

      let code = `# Auto-generated Turing Machine\n# Generated by Turing Sandbox\n\n`;
      code += `def turing_machine(input_str):\n`;
      code += `    tape = list(input_str) + ['_'] * 100\n`;
      code += `    head = 0\n`;
      code += `    current_state = ${startNode.id}\n`;
      code += `    steps = 0\n`;
      code += `    max_steps = 10000\n\n`;
      
      code += `    while steps < max_steps:\n`;
      code += `        char_under_head = tape[head]\n\n`;
      
      // Group transitions by state
      const transitionsByState: Record<number, MachineConnection[]> = {};
      connections.forEach(c => {
          if (!transitionsByState[c.start]) transitionsByState[c.start] = [];
          transitionsByState[c.start].push(c);
      });

      let firstState = true;
      Object.keys(transitionsByState).forEach(stateIdStr => {
          const stateId = parseInt(stateIdStr);
          const conns = transitionsByState[stateId];
          
          code += `        ${firstState ? 'if' : 'elif'} current_state == ${stateId}:\n`;
          firstState = false;

          let firstTrans = true;
          conns.forEach(c => {
              const reads = c.read || [];
              const cond = reads.map(r => `char_under_head == '${r}'`).join(' or ');
              
              code += `            ${firstTrans ? 'if' : 'elif'} ${cond}:\n`;
              code += `                tape[head] = '${c.write}'\n`;
              if (c.move === 'R') code += `                head += 1\n`;
              if (c.move === 'L') code += `                head = max(0, head - 1)\n`;
              code += `                current_state = ${c.end}\n`;
              
              firstTrans = false;
          });
      });

      code += `        else:\n`;
      code += `            # Halt\n`;
      code += `            break\n\n`;
      code += `        steps += 1\n\n`;
      code += `    # Check if end state is accepting\n`;
      const endStates = nodes.filter(n => n.is_end).map(n => n.id);
      code += `    accept_states = [${endStates.join(', ')}]\n`;
      code += `    return current_state in accept_states, "".join(tape).strip('_')\n\n`;
      code += `# Run\n`;
      code += `accepted, output = turing_machine("10101")\n`;
      code += `print(f"Accepted: {accepted}, Output: {output}")`;

      setGeneratedCode(code);
      setShowCodeModal(true);
  };

  const copyCode = () => {
      navigator.clipboard.writeText(generatedCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
  };

  // --- Properties Panel Updates ---
  const updateNode = (key: keyof MachineNode, value: any) => {
      if (selectedNodeId === null) return;
      setNodes(prev => prev.map(n => n.id === selectedNodeId ? { ...n, [key]: value } : n));
  };

  const updateConnection = (key: keyof MachineConnection, value: any) => {
      if (selectedConnIndex === null) return;
      setConnections(prev => prev.map((c, i) => i === selectedConnIndex ? { ...c, [key]: value } : c));
  };

  // --- Simulation Helpers ---
  const runSimulation = () => {
      // Build "workshop item" structure for simulator
      const dummyItem: any = {
          nodesJson: JSON.stringify(nodes),
          connectionsJson: JSON.stringify(connections),
          alphabetJson: JSON.stringify(["0", "1", "_"]),
          twoTapes
      };
      const sim = new TuringSimulator(dummyItem);
      setSimulator(sim);
      setSimState(sim.initSession(simInput, twoTapes));
      setIsSimulating(true);
  };

  const stepSimulation = () => {
      if (simulator && simState && simState.status === 'running') {
          setSimState(simulator.step(simState, twoTapes));
      }
  };

  const resetSimulation = () => {
      if (simulator) {
          setSimState(simulator.initSession(simInput, twoTapes));
      }
  };

  const stopSimulation = () => {
      setIsSimulating(false);
      setSimState(null);
      setSimulator(null);
  };

  // --- Render Helpers ---
  const renderConnection = (conn: MachineConnection, index: number) => {
      const start = nodes.find(n => n.id === conn.start);
      const end = nodes.find(n => n.id === conn.end);
      if (!start || !end) return null;

      const isSelected = selectedConnIndex === index;
      const isSimActive = simState?.lastTransitionId === index;
      
      let color = '#475569';
      if (isSelected) color = '#38bdf8';
      if (isSimActive) color = '#10b981';

      let d = '';
      let labelPos = { x: 0, y: 0 };

      if (start.id === end.id) {
          // Self loop
          d = `M ${start.x} ${start.y - 25} C ${start.x - 50} ${start.y - 100}, ${start.x + 50} ${start.y - 100}, ${start.x} ${start.y - 25}`;
          labelPos = { x: start.x, y: start.y - 90 };
      } else {
          // Line
          // Add curve if bidirectional
          const isBi = connections.some(c => c.start === end.id && c.end === start.id);
          if (isBi) {
              const midX = (start.x + end.x) / 2;
              const midY = (start.y + end.y) / 2;
              const dx = end.x - start.x;
              const dy = end.y - start.y;
              // Normal vector
              const nx = -dy * 0.2;
              const ny = dx * 0.2;
              d = `M ${start.x} ${start.y} Q ${midX + nx} ${midY + ny} ${end.x} ${end.y}`;
              labelPos = { x: midX + nx, y: midY + ny };
          } else {
              d = `M ${start.x} ${start.y} L ${end.x} ${end.y}`;
              labelPos = { x: (start.x + end.x) / 2, y: (start.y + end.y) / 2 };
          }
      }

      const label = twoTapes 
        ? `${conn.read?.[0]}/${conn.read2?.[0]}→${conn.write}/${conn.write2},${conn.move}${conn.move2}`
        : `${conn.read?.[0]}→${conn.write},${conn.move}`;

      return (
          <g key={index} onClick={(e) => { e.stopPropagation(); setSelectedConnIndex(index); setSelectedNodeId(null); }}>
              <path d={d} stroke={color} strokeWidth={isSelected || isSimActive ? 3 : 2} fill="none" markerEnd={`url(#arrow-${index})`} />
              <defs>
                  <marker id={`arrow-${index}`} markerWidth="10" markerHeight="7" refX="28" refY="3.5" orient="auto">
                      <polygon points="0 0, 10 3.5, 0 7" fill={color} />
                  </marker>
              </defs>
              
              <rect x={labelPos.x - (label.length * 3)} y={labelPos.y - 10} width={label.length * 6 + 10} height={16} rx={4} fill="#0f172a" stroke={color} strokeWidth={1} />
              <text x={labelPos.x} y={labelPos.y + 4} textAnchor="middle" fontSize={10} fill={isSelected ? '#38bdf8' : '#94a3b8'} className="font-mono font-bold pointer-events-none select-none">
                  {label}
              </text>
          </g>
      );
  };

  return (
    <div className="flex h-full gap-4 overflow-hidden p-6">
        {/* Toolbar */}
        <div className="w-16 bg-slate-900 border border-slate-800 rounded-xl flex flex-col items-center py-4 gap-4 shrink-0">
            <button onClick={addNode} className="p-3 bg-slate-800 text-slate-200 rounded-lg hover:bg-brand-500 hover:text-white transition-colors" title="Add Node">
                <Plus size={20} />
            </button>
            <button onClick={() => setTwoTapes(!twoTapes)} className={`p-3 rounded-lg transition-colors ${twoTapes ? 'bg-emerald-500/20 text-emerald-400' : 'bg-slate-800 text-slate-400'}`} title="Toggle 2 Tapes">
                <Disc size={20} />
            </button>
            <div className="w-8 h-px bg-slate-800" />
            <button onClick={() => setScale(s => Math.min(s + 0.1, 3))} className="p-3 text-slate-400 hover:text-white"><ZoomIn size={20} /></button>
            <button onClick={() => setScale(s => Math.max(s - 0.1, 0.2))} className="p-3 text-slate-400 hover:text-white"><ZoomOut size={20} /></button>
            <div className="flex-1" />
            <button onClick={generatePython} className="p-3 text-slate-400 hover:text-brand-400" title="Export to Python">
                <FileCode size={20} />
            </button>
            <button onClick={() => alert('Save feature placeholder')} className="p-3 text-slate-400 hover:text-brand-400"><Save size={20} /></button>
        </div>

        {/* Main Canvas */}
        <div className="flex-1 bg-[#020617] relative rounded-xl border border-slate-800 overflow-hidden shadow-inner shadow-black/50">
            {/* Grid Pattern */}
            <div className="absolute inset-0 opacity-20 pointer-events-none" style={{ 
                backgroundImage: 'linear-gradient(#1e293b 1px, transparent 1px), linear-gradient(90deg, #1e293b 1px, transparent 1px)',
                backgroundSize: `${20 * scale}px ${20 * scale}px`,
                backgroundPosition: `${offset.x}px ${offset.y}px`
            }} />

            <svg 
                ref={svgRef}
                className="w-full h-full cursor-grab active:cursor-grabbing"
                onMouseDown={handleMouseDown}
                onMouseMove={handleMouseMove}
                onMouseUp={handleMouseUp}
            >
                <g transform={`translate(${offset.x}, ${offset.y}) scale(${scale})`}>
                    {/* Connections */}
                    {connections.map((c, i) => renderConnection(c, i))}

                    {/* Drag Line */}
                    {isConnecting && connectStartId !== null && (
                        <line 
                            x1={nodes.find(n => n.id === connectStartId)?.x} 
                            y1={nodes.find(n => n.id === connectStartId)?.y}
                            x2={mousePos.x}
                            y2={mousePos.y}
                            stroke="#38bdf8"
                            strokeWidth="2"
                            strokeDasharray="5,5"
                        />
                    )}

                    {/* Nodes */}
                    {nodes.map(node => {
                        const isSelected = selectedNodeId === node.id;
                        const isSimCurrent = simState?.currentNodeId === node.id;
                        
                        return (
                        <g 
                            key={node.id} 
                            transform={`translate(${node.x}, ${node.y})`}
                            onMouseDown={(e) => handleNodeMouseDown(e, node.id)}
                            className="cursor-pointer"
                        >
                            <circle r="25" fill={isSimCurrent ? '#0ea5e9' : '#1e293b'} stroke={isSelected ? '#38bdf8' : isSimCurrent ? '#7dd3fc' : '#475569'} strokeWidth={isSelected || isSimCurrent ? 3 : 2} />
                            {node.is_end && <circle r="20" fill="none" stroke={isSimCurrent ? '#fff' : '#475569'} strokeWidth="1" />}
                            {node.is_start && <polygon points="-10,-10 -10,10 5,0" fill="#10b981" transform="translate(-25, -25)" />}
                            
                            <text textAnchor="middle" dy=".3em" fill="#e2e8f0" fontWeight="bold" fontSize="14" className="pointer-events-none select-none">
                                {node.id}
                            </text>
                            
                            {/* Connect Handle */}
                            {isSelected && !isConnecting && (
                                <circle 
                                    r="8" 
                                    cx="25" 
                                    fill="#38bdf8" 
                                    className="hover:scale-125 transition-transform"
                                    onMouseDown={(e) => startConnection(e, node.id)} 
                                />
                            )}
                        </g>
                    )})}
                </g>
            </svg>

            {/* Sim Overlay */}
            {isSimulating && (
                <div className="absolute top-4 left-4 right-4 bg-slate-900/90 backdrop-blur border border-slate-700 rounded-xl p-4 flex flex-col gap-4 shadow-xl">
                    <div className="flex justify-between items-center">
                        <div className="flex items-center gap-3">
                            <span className="text-xs font-bold uppercase text-slate-500">Status</span>
                            <span className={`font-mono font-bold ${
                                simState?.status === 'accepted' ? 'text-emerald-400' : 
                                simState?.status === 'rejected' ? 'text-red-400' : 'text-blue-400'
                            }`}>
                                {simState?.status.toUpperCase()}
                            </span>
                            <div className="h-4 w-px bg-slate-700 mx-2" />
                            <span className="text-xs font-bold uppercase text-slate-500">Step</span>
                            <span className="font-mono text-slate-200">{simState?.steps}</span>
                        </div>
                        <button onClick={stopSimulation} className="p-1 hover:text-white text-slate-400"><Trash2 size={16}/></button>
                    </div>
                    
                    {/* Tapes */}
                    <div className="space-y-2">
                        {simState && (
                             <div className="flex gap-1 overflow-x-auto pb-2">
                                <span className="text-xs text-slate-500 font-mono w-12 shrink-0 pt-2">Tape 1</span>
                                {Object.keys(simState.tape1.data).sort((a,b) => parseInt(a)-parseInt(b)).map((idx) => (
                                    <div key={idx} className={`w-8 h-8 border flex items-center justify-center shrink-0 rounded ${
                                        parseInt(idx) === simState.tape1.head 
                                        ? 'border-brand-500 bg-brand-500/20 text-brand-300' 
                                        : 'border-slate-700 bg-slate-800 text-slate-400'
                                    }`}>
                                        {simState.tape1.data[parseInt(idx)]}
                                    </div>
                                ))}
                             </div>
                        )}
                        {twoTapes && simState && (
                             <div className="flex gap-1 overflow-x-auto pb-2">
                                <span className="text-xs text-slate-500 font-mono w-12 shrink-0 pt-2">Tape 2</span>
                                {Object.keys(simState.tape2.data).sort((a,b) => parseInt(a)-parseInt(b)).map((idx) => (
                                    <div key={idx} className={`w-8 h-8 border flex items-center justify-center shrink-0 rounded ${
                                        parseInt(idx) === simState.tape2.head 
                                        ? 'border-brand-500 bg-brand-500/20 text-brand-300' 
                                        : 'border-slate-700 bg-slate-800 text-slate-400'
                                    }`}>
                                        {simState.tape2.data[parseInt(idx)]}
                                    </div>
                                ))}
                             </div>
                        )}
                    </div>

                    <div className="flex justify-center gap-4">
                        <button onClick={resetSimulation} className="p-2 bg-slate-800 rounded-lg hover:text-white text-slate-400"><RotateCcw size={18} /></button>
                        <button onClick={stepSimulation} className="px-6 py-2 bg-brand-600 hover:bg-brand-500 text-white font-bold rounded-lg flex items-center gap-2">
                            Step <ArrowRight size={16} />
                        </button>
                    </div>
                </div>
            )}
        </div>

        {/* Properties Panel */}
        <div className="w-72 bg-slate-900 border border-slate-800 rounded-xl flex flex-col shrink-0">
            <div className="p-4 border-b border-slate-800 font-bold text-slate-200 flex items-center gap-2">
                <Settings size={18} /> Properties
            </div>
            
            <div className="p-4 flex-1 overflow-y-auto">
                {selectedNodeId !== null ? (
                    <div className="space-y-4">
                        <div className="text-xs font-bold text-slate-500 uppercase">Selected Node</div>
                        <div className="text-2xl font-mono text-white mb-4">#{selectedNodeId}</div>
                        
                        <div className="space-y-2">
                            <label className="flex items-center gap-3 p-3 bg-slate-950 rounded-lg border border-slate-800 cursor-pointer hover:border-brand-500/50 transition-colors">
                                <input 
                                    type="checkbox" 
                                    checked={nodes.find(n => n.id === selectedNodeId)?.is_start}
                                    onChange={(e) => updateNode('is_start', e.target.checked)}
                                    className="w-4 h-4 rounded border-slate-600 text-brand-500 focus:ring-brand-500 bg-slate-900"
                                />
                                <span className="text-sm text-slate-300">Start State</span>
                            </label>

                            <label className="flex items-center gap-3 p-3 bg-slate-950 rounded-lg border border-slate-800 cursor-pointer hover:border-brand-500/50 transition-colors">
                                <input 
                                    type="checkbox" 
                                    checked={nodes.find(n => n.id === selectedNodeId)?.is_end}
                                    onChange={(e) => updateNode('is_end', e.target.checked)}
                                    className="w-4 h-4 rounded border-slate-600 text-brand-500 focus:ring-brand-500 bg-slate-900"
                                />
                                <span className="text-sm text-slate-300">Accept State</span>
                            </label>
                        </div>

                        <button onClick={deleteSelected} className="w-full mt-8 py-2 bg-red-500/10 text-red-400 border border-red-500/20 rounded-lg hover:bg-red-500/20 transition-colors flex items-center justify-center gap-2 text-sm font-bold">
                            <Trash2 size={16} /> Delete Node
                        </button>
                    </div>
                ) : selectedConnIndex !== null ? (
                    <div className="space-y-4">
                        <div className="text-xs font-bold text-slate-500 uppercase">Transition</div>
                        
                        <div className="space-y-3">
                            <div>
                                <label className="text-xs text-slate-400 block mb-1">Read (Tape 1)</label>
                                <input 
                                    className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                    value={connections[selectedConnIndex].read?.[0] || ''}
                                    onChange={(e) => updateConnection('read', [e.target.value])}
                                    maxLength={1}
                                />
                            </div>
                            <div className="grid grid-cols-2 gap-2">
                                <div>
                                    <label className="text-xs text-slate-400 block mb-1">Write</label>
                                    <input 
                                        className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                        value={connections[selectedConnIndex].write || ''}
                                        onChange={(e) => updateConnection('write', e.target.value)}
                                        maxLength={1}
                                    />
                                </div>
                                <div>
                                    <label className="text-xs text-slate-400 block mb-1">Move</label>
                                    <select 
                                        className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                        value={connections[selectedConnIndex].move || 'R'}
                                        onChange={(e) => updateConnection('move', e.target.value)}
                                    >
                                        <option value="L">Left</option>
                                        <option value="R">Right</option>
                                        <option value="S">Stay</option>
                                    </select>
                                </div>
                            </div>
                        </div>

                        {twoTapes && (
                            <div className="pt-4 border-t border-slate-800 space-y-3">
                                <div className="text-xs font-bold text-slate-500 uppercase">Tape 2 Rules</div>
                                <div>
                                    <label className="text-xs text-slate-400 block mb-1">Read</label>
                                    <input 
                                        className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                        value={connections[selectedConnIndex].read2?.[0] || ''}
                                        onChange={(e) => updateConnection('read2', [e.target.value])}
                                        maxLength={1}
                                    />
                                </div>
                                <div className="grid grid-cols-2 gap-2">
                                    <div>
                                        <label className="text-xs text-slate-400 block mb-1">Write</label>
                                        <input 
                                            className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                            value={connections[selectedConnIndex].write2 || ''}
                                            onChange={(e) => updateConnection('write2', e.target.value)}
                                            maxLength={1}
                                        />
                                    </div>
                                    <div>
                                        <label className="text-xs text-slate-400 block mb-1">Move</label>
                                        <select 
                                            className="w-full bg-slate-950 border border-slate-700 rounded p-2 text-white font-mono text-sm"
                                            value={connections[selectedConnIndex].move2 || 'S'}
                                            onChange={(e) => updateConnection('move2', e.target.value)}
                                        >
                                            <option value="L">Left</option>
                                            <option value="R">Right</option>
                                            <option value="S">Stay</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                        )}

                        <button onClick={deleteSelected} className="w-full mt-8 py-2 bg-red-500/10 text-red-400 border border-red-500/20 rounded-lg hover:bg-red-500/20 transition-colors flex items-center justify-center gap-2 text-sm font-bold">
                            <Trash2 size={16} /> Delete Connection
                        </button>
                    </div>
                ) : (
                    <div className="text-center text-slate-500 text-sm mt-10">
                        <MousePointer2 className="mx-auto mb-2 opacity-50" size={32} />
                        Select a node or connection to edit properties.
                    </div>
                )}
            </div>

            {/* Sim Control in Panel */}
            <div className="p-4 border-t border-slate-800 bg-slate-950">
                <div className="text-xs font-bold text-slate-500 uppercase mb-2">Quick Test</div>
                <input 
                    className="w-full bg-slate-900 border border-slate-700 rounded p-2 text-white font-mono text-sm mb-2 placeholder-slate-600"
                    placeholder="Input string..."
                    value={simInput}
                    onChange={(e) => setSimInput(e.target.value)}
                />
                {!isSimulating ? (
                    <button onClick={runSimulation} className="w-full py-2 bg-brand-600 hover:bg-brand-500 text-white rounded-lg font-bold text-sm transition-colors flex items-center justify-center gap-2">
                        <Play size={16} fill="currentColor" /> Test Run
                    </button>
                ) : (
                    <button onClick={stopSimulation} className="w-full py-2 bg-slate-800 hover:bg-slate-700 text-slate-300 rounded-lg font-bold text-sm transition-colors">
                        Stop Test
                    </button>
                )}
            </div>
        </div>

        {/* Generated Code Modal */}
        <Modal 
            isOpen={showCodeModal} 
            onClose={() => setShowCodeModal(false)}
            title="Export to Python"
        >
            <div className="relative">
                <pre className="bg-[#0d1117] p-4 rounded-lg border border-slate-800 text-xs font-mono text-slate-300 overflow-auto max-h-[500px] custom-scrollbar">
                    {generatedCode}
                </pre>
                <button 
                    onClick={copyCode}
                    className="absolute top-4 right-4 p-2 bg-slate-800 hover:bg-brand-600 text-white rounded-lg transition-colors flex items-center gap-2 text-xs font-bold shadow-lg"
                >
                    {copied ? <Check size={14} /> : <FileCode size={14} />}
                    {copied ? 'Copied!' : 'Copy to Clipboard'}
                </button>
            </div>
        </Modal>
    </div>
  );
};
