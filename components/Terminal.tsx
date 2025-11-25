import React, { useState, useEffect, useRef } from 'react';
import { X, Terminal as TerminalIcon } from 'lucide-react';
import { api } from '../services/api';

interface TerminalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const Terminal: React.FC<TerminalProps> = ({ isOpen, onClose }) => {
  const [input, setInput] = useState('');
  const [history, setHistory] = useState<string[]>([
    'Turing Machine API Console [v2.4.0]',
    'Type "help" for available commands.',
    ' '
  ]);
  const [isProcessing, setIsProcessing] = useState(false);
  const bottomRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (isOpen && bottomRef.current) {
      bottomRef.current.scrollIntoView({ behavior: 'smooth' });
      inputRef.current?.focus();
    }
  }, [history, isOpen]);

  useEffect(() => {
    const handleEsc = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) onClose();
    };
    window.addEventListener('keydown', handleEsc);
    return () => window.removeEventListener('keydown', handleEsc);
  }, [isOpen, onClose]);

  const addToHistory = (text: string, isCommand = false) => {
    setHistory(prev => [...prev, isCommand ? `> ${text}` : text]);
  };

  const executeCommand = async (cmd: string) => {
    const cleanCmd = cmd.trim().toLowerCase();
    const args = cleanCmd.split(' ');
    const command = args[0];

    setIsProcessing(true);

    try {
      switch (command) {
        case 'help':
          addToHistory('  help        - Show this message');
          addToHistory('  clear       - Clear console');
          addToHistory('  status      - Check API health');
          addToHistory('  lobbies     - List active lobbies');
          addToHistory('  players     - List recent players');
          addToHistory('  whoami      - Current session info');
          break;

        case 'clear':
          setHistory([]);
          break;

        case 'whoami':
          addToHistory('  User: Administrator');
          addToHistory('  Access: Full Control');
          break;
          
        case 'status':
          const health = await api.health();
          addToHistory(`  Status: ${health.status}`);
          break;

        case 'lobbies':
          const lobbyList = await api.lobbies.getAll();
          if (lobbyList.length === 0) {
            addToHistory('  No active lobbies.');
          } else {
            addToHistory(`  Count: ${lobbyList.length}`);
            lobbyList.forEach(l => {
              addToHistory(`  - ${l.code} | ${l.name} | ${l.lobbyPlayers?.length || 0}/${l.maxPlayers}`);
            });
          }
          break;

        case 'players':
          const players = await api.players.getAll();
          addToHistory(`  Total: ${players.length}`);
          players.slice(0, 5).forEach(p => {
            addToHistory(`  - [${p.id}] ${p.username} (${p.role})`);
          });
          if (players.length > 5) addToHistory(`  ... ${players.length - 5} more`);
          break;

        case '':
          break;

        default:
          addToHistory(`  Unknown command: ${command}`);
      }
    } catch (e) {
      addToHistory('  Error: Execution failed');
    } finally {
      setIsProcessing(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!input) return;
    
    const cmd = input;
    addToHistory(cmd, true);
    setInput('');
    await executeCommand(cmd);
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-start justify-center pt-20 bg-black/40 backdrop-blur-sm" onClick={onClose}>
      <div 
        className="w-full max-w-2xl bg-[#0d1117] border border-slate-700 rounded-lg shadow-2xl overflow-hidden flex flex-col h-[50vh] animate-in fade-in slide-in-from-top-4 duration-200"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="bg-slate-800/50 px-4 py-2 flex items-center justify-between border-b border-slate-700">
          <div className="flex items-center gap-2 text-slate-400 text-xs font-medium">
            <TerminalIcon size={14} />
            Console
          </div>
          <div className="text-[10px] text-slate-600 font-mono">ESC to close</div>
        </div>

        {/* Output Area */}
        <div className="flex-1 p-4 overflow-y-auto font-mono text-sm space-y-1 custom-scrollbar text-slate-300">
          {history.map((line, i) => (
            <div key={i} className={`${line.startsWith('>') ? 'text-slate-500 mt-2' : ''}`}>
              {line}
            </div>
          ))}
          <div ref={bottomRef} />
        </div>

        {/* Input Area */}
        <form onSubmit={handleSubmit} className="bg-[#0d1117] p-3 border-t border-slate-700 flex items-center gap-2">
          <span className="text-slate-500 font-mono text-sm font-bold shrink-0">{'>'}</span>
          <input
            ref={inputRef}
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            className="flex-1 bg-transparent border-none outline-none text-slate-200 font-mono text-sm placeholder-slate-700"
            autoFocus
            disabled={isProcessing}
          />
        </form>
      </div>
    </div>
  );
};