import React, { useState, useEffect, useContext, createContext } from 'react';
import { HashRouter as Router, Routes, Route, Navigate, NavLink, useNavigate } from 'react-router-dom';
import { LayoutDashboard, Users, Box, Server, LogOut, Menu, X, Terminal as TerminalIcon, PlayCircle, ShieldAlert, UserCircle, ScrollText, Trophy } from 'lucide-react';
import { Dashboard } from './pages/Dashboard';
import { Players } from './pages/Players';
import { Workshop } from './pages/Workshop';
import { Lobbies } from './pages/Lobbies';
import { Simulator } from './pages/Simulator';
import { Login } from './pages/Login';
import { Profile } from './pages/Profile';
import { Leaderboard } from './pages/Leaderboard';
import { AdminLogs } from './pages/AdminLogs';
import { Terminal } from './components/Terminal';
import { Player } from './types';
import { api } from './services/api';

// --- User Context ---
export const UserContext = createContext<{
  user: Player | null;
  loading: boolean;
  refreshUser: () => void;
}>({ user: null, loading: true, refreshUser: () => {} });

// --- Auth Wrapper ---
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const token = localStorage.getItem('turing_admin_token');
  if (!token) return <Navigate to="/login" replace />;
  return <>{children}</>;
};

// --- Admin Wrapper ---
const AdminRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { user, loading } = useContext(UserContext);
  if (loading) return null; 
  if (!user || user.role !== 'Admin') return <Navigate to="/" replace />;
  return <>{children}</>;
};

// --- Sidebar ---
const Sidebar = ({ onOpenTerminal }: { onOpenTerminal: () => void }) => {
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false); // Mobile toggle state
  const { user } = useContext(UserContext);

  const handleLogout = () => {
    localStorage.removeItem('turing_admin_token');
    navigate('/login');
  };

  const navItems = [
    { path: '/', icon: LayoutDashboard, label: 'Dashboard' },
    { path: '/workshop', icon: Box, label: 'Workshop' },
    { path: '/simulator', icon: PlayCircle, label: 'Simulator' },
    { path: '/leaderboard', icon: Trophy, label: 'Leaderboard' },
    { path: '/lobbies', icon: Server, label: 'Lobbies' },
    // Only show Players if Admin
    { path: '/players', icon: Users, label: 'Players', adminOnly: true },
    { path: '/logs', icon: ScrollText, label: 'Admin Logs', adminOnly: true },
  ];

  return (
    <>
      {/* Mobile Header */}
      <div className="lg:hidden h-16 bg-slate-950 border-b border-slate-800 flex items-center justify-between px-4 fixed top-0 left-0 right-0 z-40">
         <div className="font-bold text-lg text-white">Turing<span className="text-brand-500">Sandbox</span></div>
         <button onClick={() => setIsOpen(!isOpen)} className="p-2 text-slate-400">
            {isOpen ? <X size={24} /> : <Menu size={24} />}
         </button>
      </div>

      {/* Sidebar Container */}
      <aside className={`
        fixed lg:static inset-y-0 left-0 z-50 w-64 bg-slate-950 border-r border-slate-800 
        transform transition-transform duration-200 ease-in-out
        ${isOpen ? 'translate-x-0' : '-translate-x-full lg:translate-x-0'}
        flex flex-col
      `}>
        <div className="h-16 flex items-center px-6 border-b border-slate-800">
          <div className="font-bold text-lg text-white tracking-tight">Turing<span className="text-brand-500">Sandbox</span></div>
        </div>

        <nav className="flex-1 p-4 space-y-1 overflow-y-auto custom-scrollbar">
          <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-4 px-2 mt-2">Menu</div>
          {navItems.map((item) => {
            if (item.adminOnly && user?.role !== 'Admin') return null;
            
            return (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={() => setIsOpen(false)}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                  isActive 
                    ? 'bg-brand-500/10 text-brand-500' 
                    : 'text-slate-400 hover:text-slate-100 hover:bg-slate-900'
                }`
              }
            >
              <item.icon size={18} />
              {item.label}
            </NavLink>
          )})}
          
          <div className="pt-4 mt-4 border-t border-slate-800">
             <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 px-2">Account</div>
             <NavLink
               to="/profile"
               onClick={() => setIsOpen(false)}
               className={({ isActive }) =>
                 `flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                   isActive 
                     ? 'bg-brand-500/10 text-brand-500' 
                     : 'text-slate-400 hover:text-slate-100 hover:bg-slate-900'
                 }`
               }
             >
               <UserCircle size={18} />
               My Profile
             </NavLink>
             
             <div className="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2 px-2 mt-4">Tools</div>
             <button
              onClick={() => {
                  onOpenTerminal();
                  setIsOpen(false);
              }}
              className="w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm font-medium text-slate-400 hover:text-slate-100 hover:bg-slate-900 transition-colors text-left"
            >
               <TerminalIcon size={18} />
               Terminal
            </button>
          </div>
        </nav>

        <div className="p-4 border-t border-slate-800">
          {user && (
             <div className="px-3 py-2 mb-2 text-xs text-slate-500">
               Logged in as <span className="text-slate-300 font-medium">{user.username}</span>
               <div className="flex items-center gap-1.5 mt-1">
                 <div className={`w-1.5 h-1.5 rounded-full ${user.role === 'Admin' ? 'bg-purple-500' : 'bg-brand-500'}`}></div>
                 <span className="text-[10px] uppercase text-slate-400">{user.role}</span>
               </div>
             </div>
          )}
          <button 
            onClick={handleLogout}
            className="flex items-center gap-3 w-full px-3 py-2 rounded-md text-sm font-medium text-slate-400 hover:text-red-400 hover:bg-red-500/5 transition-colors"
          >
            <LogOut size={18} />
            Sign out
          </button>
        </div>
      </aside>
      
      {/* Overlay for mobile */}
      {isOpen && (
        <div 
            className="fixed inset-0 bg-slate-950/80 z-40 lg:hidden"
            onClick={() => setIsOpen(false)}
        ></div>
      )}
    </>
  );
};

const AppContent = () => {
    const [isTerminalOpen, setIsTerminalOpen] = useState(false);
    const [user, setUser] = useState<Player | null>(null);
    const [loading, setLoading] = useState(true);

    const fetchUser = async () => {
       // Step 1: Try to parse token locally first for immediate UI
       const localUser = api.auth.getLocalUser();
       if (localUser) {
          setUser(localUser);
          setLoading(false); // UI is ready
       }

       // Step 2: Verify with server to ensure token is not revoked
       try {
          const serverUser = await api.auth.verify();
          setUser(serverUser);
       } catch (e: any) {
          if (e.message === 'Unauthorized') {
              console.warn("Session expired or invalid.");
              localStorage.removeItem('turing_admin_token');
              setUser(null);
          } else {
            if (!localUser) setUser(null);
          }
       } finally {
          setLoading(false);
       }
    };

    useEffect(() => {
      fetchUser();
    }, []);

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === '`' && !e.ctrlKey && !e.altKey && !e.metaKey) {
                e.preventDefault(); 
                setIsTerminalOpen(prev => !prev);
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, []);

    return (
      <UserContext.Provider value={{ user, loading, refreshUser: fetchUser }}>
        <div className="flex h-screen bg-slate-950 text-slate-200 font-sans selection:bg-brand-500/30 overflow-hidden">
            <Routes>
                <Route path="/login" element={<Login />} />
                <Route
                path="/*"
                element={
                    <ProtectedRoute>
                        <>
                            <Sidebar onOpenTerminal={() => setIsTerminalOpen(true)} />
                            <main className="flex-1 flex flex-col min-h-0 bg-slate-950 pt-16 lg:pt-0 relative">
                                <Routes>
                                    <Route path="/" element={<Dashboard />} />
                                    <Route path="/workshop" element={<Workshop />} />
                                    <Route path="/simulator" element={<Simulator />} />
                                    <Route path="/leaderboard" element={<Leaderboard />} />
                                    <Route path="/lobbies" element={<Lobbies />} />
                                    <Route path="/profile" element={<Profile />} />
                                    <Route path="/players" element={
                                        <AdminRoute>
                                        <Players />
                                        </AdminRoute>
                                    } />
                                    <Route path="/logs" element={
                                        <AdminRoute>
                                        <AdminLogs />
                                        </AdminRoute>
                                    } />
                                </Routes>
                            </main>
                            <Terminal isOpen={isTerminalOpen} onClose={() => setIsTerminalOpen(false)} />
                        </>
                    </ProtectedRoute>
                }
                />
            </Routes>
        </div>
      </UserContext.Provider>
    );
}

const App: React.FC = () => {
  return (
    <Router>
      <AppContent />
    </Router>
  );
};

export default App;