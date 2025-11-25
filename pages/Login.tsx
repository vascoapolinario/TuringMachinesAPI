
import React, { useState, useContext } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { Lock, User, AlertCircle, ArrowRight, Cpu, UserPlus, Eye, EyeOff } from 'lucide-react';
import { UserContext } from '../App';

export const Login: React.FC = () => {
  const [isRegistering, setIsRegistering] = useState(false);
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();
  const { refreshUser } = useContext(UserContext);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      if (isRegistering) {
        // Register flow
        await api.auth.register(username, password);
        // On success, automatically login
        const response = await api.auth.login(username, password);
        handleAuthSuccess(response);
      } else {
        // Login flow
        const response = await api.auth.login(username, password);
        handleAuthSuccess(response);
      }
    } catch (err: any) {
      if (isRegistering) {
        setError(err.message || 'Registration failed. Username might be taken.');
      } else {
        setError('Invalid credentials. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleAuthSuccess = async (response: any) => {
      if (response && response.token) {
        localStorage.setItem('turing_admin_token', response.token);
        // Refresh user context immediately so the app knows we are an admin
        await refreshUser();
        navigate('/');
      } else {
        // Handle legacy string response if applicable
        if (typeof response === 'string') {
            localStorage.setItem('turing_admin_token', response);
            await refreshUser();
            navigate('/');
        } else {
           throw new Error("Invalid response format");
        }
      }
  };

  return (
    <div className="min-h-screen w-full bg-slate-950 flex items-center justify-center p-4 relative overflow-hidden">
      {/* Background Effects */}
      <div className="absolute inset-0 bg-[linear-gradient(to_right,#1e293b_1px,transparent_1px),linear-gradient(to_bottom,#1e293b_1px,transparent_1px)] bg-[size:4rem_4rem] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)] opacity-20"></div>
      <div className="absolute top-0 left-0 w-full h-full bg-slate-950/80"></div>
      
      <div className="w-full max-w-md animate-in fade-in zoom-in-95 duration-500 relative z-10">
        <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-20 h-20 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 border border-slate-700 mb-6 shadow-2xl shadow-black/50 ring-1 ring-white/10">
                <Cpu size={40} className="text-brand-400 drop-shadow-lg" />
            </div>
            <h1 className="text-4xl font-bold text-white tracking-tight drop-shadow-md">Turing Sandbox</h1>
            <p className="text-slate-400 mt-3 text-sm font-medium">
                {isRegistering ? 'Create your account' : 'Sign in to continue'}
            </p>
        </div>

        <div className="bg-slate-900/60 backdrop-blur-xl border border-white/10 rounded-3xl shadow-2xl p-8 ring-1 ring-black/5">
            
            {/* Tabs */}
            <div className="flex bg-slate-950/50 p-1 rounded-xl mb-8 border border-white/5">
                <button 
                    onClick={() => { setIsRegistering(false); setError(''); }}
                    className={`flex-1 py-2 text-sm font-bold rounded-lg transition-all ${!isRegistering ? 'bg-slate-800 text-white shadow-lg' : 'text-slate-500 hover:text-slate-300'}`}
                >
                    Sign In
                </button>
                <button 
                    onClick={() => { setIsRegistering(true); setError(''); }}
                    className={`flex-1 py-2 text-sm font-bold rounded-lg transition-all ${isRegistering ? 'bg-slate-800 text-white shadow-lg' : 'text-slate-500 hover:text-slate-300'}`}
                >
                    Register
                </button>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
            {error && (
                <div className="bg-red-500/10 border border-red-500/20 p-4 rounded-xl flex items-center gap-3 text-red-400 text-xs font-bold animate-in fade-in slide-in-from-top-2">
                    <AlertCircle size={16} />
                    {error}
                </div>
            )}

            <div>
                <label className="block text-xs font-bold text-slate-400 uppercase tracking-wider mb-2 ml-1">Username</label>
                <div className="relative group">
                    <div className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors">
                        <User size={20} />
                    </div>
                    <input 
                        type="text" 
                        required
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        className="w-full bg-slate-950/80 border border-slate-700/50 rounded-xl py-3.5 pl-12 pr-4 text-slate-200 text-sm focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all outline-none placeholder:text-slate-600 font-mono"
                        placeholder="Enter username"
                        minLength={3}
                    />
                </div>
            </div>
            
            <div>
                <label className="block text-xs font-bold text-slate-400 uppercase tracking-wider mb-2 ml-1">Password</label>
                <div className="relative group">
                    <div className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-500 group-focus-within:text-brand-400 transition-colors">
                        <Lock size={20} />
                    </div>
                    <input 
                        type={showPassword ? "text" : "password"}
                        required
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        className="w-full bg-slate-950/80 border border-slate-700/50 rounded-xl py-3.5 pl-12 pr-12 text-slate-200 text-sm focus:ring-2 focus:ring-brand-500/20 focus:border-brand-500 transition-all outline-none placeholder:text-slate-600 font-mono"
                        placeholder="Enter password"
                        minLength={6}
                    />
                    <button 
                        type="button"
                        onClick={() => setShowPassword(!showPassword)}
                        className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-300 transition-colors"
                    >
                        {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
                    </button>
                </div>
            </div>

            <button 
                type="submit" 
                disabled={loading}
                className={`w-full font-bold py-3.5 rounded-xl transition-all shadow-xl flex items-center justify-center gap-2 disabled:opacity-70 disabled:cursor-not-allowed mt-4 transform active:scale-[0.98] ${
                    isRegistering 
                    ? 'bg-gradient-to-r from-purple-600 to-purple-500 hover:from-purple-500 hover:to-purple-400 text-white shadow-purple-500/20' 
                    : 'bg-gradient-to-r from-brand-600 to-brand-500 hover:from-brand-500 hover:to-brand-400 text-white shadow-brand-500/20'
                }`}
            >
                {loading ? 'Processing...' : (isRegistering ? 'Create Account' : 'Sign In')}
                {!loading && (isRegistering ? <UserPlus size={18} /> : <ArrowRight size={18} />)}
            </button>
            </form>
        </div>
        
        <div className="text-center mt-8 text-[10px] text-slate-600 font-mono uppercase tracking-widest">
            Turing Sandbox • Community Platform
        </div>
      </div>
    </div>
  );
};
