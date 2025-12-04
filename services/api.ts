
import { AuthResponse, Player, WorkshopItem, WorkshopItemDetail, Lobby, HealthStatus, LeaderboardEntry, AdminLog } from '../types';
import { API_ENDPOINTS, API_BASE_URL } from '../constants';

// --- Caching System ---
const apiCache: Record<string, any> = {};

const clearCache = (keyPart?: string) => {
  if (!keyPart) {
    Object.keys(apiCache).forEach(key => delete apiCache[key]);
  } else {
    Object.keys(apiCache).forEach(key => {
      if (key.includes(keyPart)) delete apiCache[key];
    });
  }
};
// ----------------------

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
      clearCache(); // Clear all cache on new login
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
      clearCache(API_ENDPOINTS.PLAYERS);
      return res.json();
    },
    verify: async (): Promise<Player> => {
      const res = await fetch(API_ENDPOINTS.VERIFY, {
        headers: getHeaders(),
      });
      if (res.status === 401) throw new Error('Unauthorized');
      if (!res.ok) throw new Error('Token invalid');
      
      const data = await res.json();
      const userData = data.user || data;
      
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
    getAll: async (forceRefresh = false): Promise<Player[]> => {
      const url = API_ENDPOINTS.PLAYERS;
      if (!forceRefresh && apiCache[url]) return apiCache[url];

      const res = await fetch(url, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch players');
      const data = await res.json();
      const result = Array.isArray(data) ? data : [];
      apiCache[url] = result;
      return result;
    },
    delete: async (id: number) => {
      const res = await fetch(`${API_ENDPOINTS.PLAYERS}/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete player');
      clearCache(API_ENDPOINTS.PLAYERS);
    }
  },
  workshop: {
    getAll: async (forceRefresh = false): Promise<WorkshopItem[]> => {
      const url = API_ENDPOINTS.WORKSHOP;
      if (!forceRefresh && apiCache[url]) return apiCache[url];

      const res = await fetch(url, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch workshop items');
      const data = await res.json();
      const result = Array.isArray(data) ? data : [];
      apiCache[url] = result;
      return result;
    },
    get: async (id: number): Promise<WorkshopItemDetail> => {
      // Detail views usually don't need heavy caching or can be cached by ID
      // For now, we fetch fresh to ensure we get details not present in getAll
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
      clearCache(API_ENDPOINTS.WORKSHOP);
    },
    rate: async (id: number, rating: number) => {
      const res = await fetch(`${API_ENDPOINTS.WORKSHOP}/${id}/rate/${rating}`, {
        method: 'POST',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to rate item');
      clearCache(API_ENDPOINTS.WORKSHOP);
    }
  },
  lobbies: {
    getAll: async (forceRefresh = false): Promise<Lobby[]> => {
      const url = API_ENDPOINTS.LOBBIES;
      if (!forceRefresh && apiCache[url]) return apiCache[url];

      const res = await fetch(url, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch lobbies');
      const data = await res.json();
      const result = Array.isArray(data) ? data : [];
      apiCache[url] = result;
      return result;
    },
    delete: async (code: string) => {
      const res = await fetch(`${API_ENDPOINTS.LOBBIES}/${code}`, {
        method: 'DELETE',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to delete lobby');
      clearCache(API_ENDPOINTS.LOBBIES);
    },
    kick: async (code: string, playerName: string) => {
      const res = await fetch(`${API_ENDPOINTS.LOBBIES}/${code}/kick/${playerName}`, {
        method: 'POST',
        headers: getHeaders(),
      });
      if (!res.ok) throw new Error('Failed to kick player');
      clearCache(API_ENDPOINTS.LOBBIES);
    }
  },
  leaderboard: {
    get: async (playerOnly: boolean = false, levelName?: string, forceRefresh = false): Promise<LeaderboardEntry[]> => {
      const params = new URLSearchParams();
      if (playerOnly) params.append('Player', 'true');
      if (levelName) params.append('levelName', levelName);

      const url = `${API_BASE_URL}/leaderboard?${params.toString()}`;
      if (!forceRefresh && apiCache[url]) return apiCache[url];

      const res = await fetch(url, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch leaderboard');
      const data = await res.json();
      const result = Array.isArray(data) ? data : [];
      apiCache[url] = result;
      return result;
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
      clearCache('leaderboard');
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
      clearCache('leaderboard');
    }
  },
  logs: {
    getAll: async (forceRefresh = false): Promise<AdminLog[]> => {
      const url = `${API_BASE_URL}/logs`;
      if (!forceRefresh && apiCache[url]) return apiCache[url];

      const res = await fetch(url, { headers: getHeaders() });
      if (!res.ok) throw new Error('Failed to fetch logs');
      const data = await res.json();
      const result = Array.isArray(data) ? data : [];
      apiCache[url] = result;
      return result;
    },
    getByActor: async (actorName: string, forceRefresh = false): Promise<AdminLog[]> => {
       const url = `${API_BASE_URL}/logs/actor/${actorName}`;
       if (!forceRefresh && apiCache[url]) return apiCache[url];

       const res = await fetch(url, { headers: getHeaders() });
       if (!res.ok) throw new Error('Failed to fetch actor logs');
       const data = await res.json();
       const result = Array.isArray(data) ? data : [];
       apiCache[url] = result;
       return result;
    },
    delete: async (id: number) => {
       const res = await fetch(`${API_BASE_URL}/logs/${id}`, {
           method: 'DELETE',
           headers: getHeaders()
       });
       if (!res.ok) throw new Error('Failed to delete log');
       clearCache('logs');
    },
    bulkDelete: async (timeSpan?: string) => {
       const url = timeSpan ? `${API_BASE_URL}/logs?timeSpan=${timeSpan}` : `${API_BASE_URL}/logs`;
       const res = await fetch(url, {
           method: 'DELETE',
           headers: getHeaders()
       });
       if (!res.ok) throw new Error('Failed to bulk delete logs');
       clearCache('logs');
    }
  }
};
