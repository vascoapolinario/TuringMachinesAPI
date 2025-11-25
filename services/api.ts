
import { AuthResponse, Player, WorkshopItem, WorkshopItemDetail, Lobby, HealthStatus, LeaderboardEntry, AdminLog } from '../types';
import { API_ENDPOINTS, API_BASE_URL } from '../constants';

// Helper to get token
const getToken = () => localStorage.getItem('turing_admin_token');

// Helper for headers
const getHeaders = () => {
  const token = getToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
  };
};

// Helper to parse JWT
const parseJwt = (token: string) => {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(window.atob(base64).split('').map(function(c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
    return JSON.parse(jsonPayload);
  } catch (e) {
    return null;
  }
};

export const api = {
  auth: {
    login: async (username: string, password: string): Promise<AuthResponse> => {
      const res = await fetch(API_ENDPOINTS.LOGIN, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      if (!res.ok) throw new Error('Login failed');
      const data = await res.json();
      return data;
    },
    register: async (username: string, password: string): Promise<Player> => {
      const res = await fetch(API_ENDPOINTS.PLAYERS, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      });
      if (!res.ok) {
          const text = await res.text();
          throw new Error(text || 'Registration failed');
      }
      return res.json();
    },
    verify: async (): Promise<Player> => {
      const res = await fetch(API_ENDPOINTS.VERIFY, {
        headers: getHeaders(),
      });
      if (res.status === 401) throw new Error('Unauthorized');
      if (!res.ok) throw new Error('Token invalid');
      
      const data = await res.json();
      // Fix: Unwrap the user object if nested (API returns { valid: true, user: {...} })
      const userData = data.user || data;
      
      // Fix: Ensure ID is a number
      if (userData && userData.id) {
          userData.id = typeof userData.id === 'string' ? parseInt(userData.id) : userData.id;
      }
      
      return userData;
    },
    getLocalUser: (): Player | null => {
      const token = getToken();
      if (!token) return null;
      
      const claims = parseJwt(token);
      if (!claims) return null;

      // Map ASP.NET Core Identity claims to Player object
      // Claims usually use these URIs or short names depending on server config
      const name = claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || claims['unique_name'] || claims['sub'];
      const role = claims['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || claims['role'] || 'User';
      const id = claims['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] || claims['nameid'] || claims['id'] || 0;

      return {
        id: parseInt(id.toString()),
        username: name || 'Unknown',
        role: role,
        createdAt: new Date().toISOString(),
        lastLogin: new Date().toISOString()
      };
    }
  },
  health: async (): Promise<HealthStatus> => {
    const res = await fetch(API_ENDPOINTS.HEALTH);
    return res.json();
  },
  players: {
    getAll: async (): Promise<Player[]> => {
      const res = await fetch(API_ENDPOINTS.PLAYERS, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch players');
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    delete: async (id: number) => {
      const res = await fetch(`${API_ENDPOINTS.PLAYERS}/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete player');
    }
  },
  workshop: {
    getAll: async (): Promise<WorkshopItem[]> => {
      const res = await fetch(API_ENDPOINTS.WORKSHOP, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch workshop items');
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    get: async (id: number): Promise<WorkshopItemDetail> => {
      const res = await fetch(`${API_ENDPOINTS.WORKSHOP}/${id}`, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch workshop item details');
      return res.json();
    },
    delete: async (id: number) => {
      const res = await fetch(`${API_ENDPOINTS.WORKSHOP}/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete item');
    },
    rate: async (id: number, rating: number) => {
      const res = await fetch(`${API_ENDPOINTS.WORKSHOP}/${id}/rate/${rating}`, {
        method: 'POST',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to rate item');
    }
  },
  lobbies: {
    getAll: async (): Promise<Lobby[]> => {
      const res = await fetch(API_ENDPOINTS.LOBBIES, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch lobbies');
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    delete: async (code: string) => {
      const res = await fetch(`${API_ENDPOINTS.LOBBIES}/${code}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete lobby');
    },
    kick: async (code: string, playerName: string) => {
      const res = await fetch(`${API_ENDPOINTS.LOBBIES}/${code}/kick/${playerName}`, {
        method: 'POST',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to kick player');
    }
  },
  leaderboard: {
    get: async (playerOnly: boolean = false, levelName?: string): Promise<LeaderboardEntry[]> => {
      const params = new URLSearchParams();
      if (playerOnly) params.append('Player', 'true');
      if (levelName) params.append('levelName', levelName);

      const res = await fetch(`${API_BASE_URL}/leaderboard?${params.toString()}`, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch leaderboard');
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    registerLevel: async (name: string, category: string, workshopItemId?: number) => {
      const payload: any = { name, category };
      if (workshopItemId) payload.workshopItemId = workshopItemId;

      const res = await fetch(`${API_BASE_URL}/leaderboard/level`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(payload)
      });
      if (!res.ok) throw new Error('Failed to register level');
    },
    deleteSubmission: async (playerName: string, levelName: string) => {
      const params = new URLSearchParams();
      params.append('playerName', playerName);
      params.append('levelName', levelName);
      
      const res = await fetch(`${API_BASE_URL}/leaderboard?${params.toString()}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete submission');
    }
  },
  logs: {
    getAll: async (): Promise<AdminLog[]> => {
      const res = await fetch(`${API_BASE_URL}/logs`, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch logs');
      const data = await res.json();
      return Array.isArray(data) ? data : [];
    },
    getByActor: async (actorName: string): Promise<AdminLog[]> => {
       const res = await fetch(`${API_BASE_URL}/logs/actor/${actorName}`, { headers: getHeaders() });
       if (!res.ok) throw new Error('Failed to fetch actor logs');
       const data = await res.json();
       return Array.isArray(data) ? data : [];
    },
    delete: async (id: number) => {
       const res = await fetch(`${API_BASE_URL}/logs/${id}`, {
           method: 'DELETE',
           headers: getHeaders()
       });
       if (!res.ok) throw new Error('Failed to delete log');
    },
    bulkDelete: async (timeSpan?: string) => {
       const url = timeSpan ? `${API_BASE_URL}/logs?timeSpan=${timeSpan}` : `${API_BASE_URL}/logs`;
       const res = await fetch(url, {
           method: 'DELETE',
           headers: getHeaders()
       });
       if (!res.ok) throw new Error('Failed to bulk delete logs');
    }
  }
};
