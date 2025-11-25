
export interface Player {
  id: number;
  username: string;
  role: string;
  createdAt: string;
  lastLogin: string | null;
}

export interface WorkshopItem {
  id: number;
  name: string;
  description: string;
  author: string;
  subscribers: number;
  rating: number;
  userRating: number;
  userIsSubscribed: boolean;
  type: string; // "Level" or "Machine"
  
  // Optional fields that may be present in the JSON response for rich display
  mode?: string; // "accept" or "transform"
  alphabetJson?: string;
  levelType?: string;
  detailedDescription?: string;
  twoTapes?: boolean;
}

// Extended interface for full details including JSON content
export interface WorkshopItemDetail extends WorkshopItem {
  levelId?: number;
  machineId?: number;
  objective?: string;
  nodesJson?: string;
  connectionsJson?: string;
  twoTapes?: boolean;
  transformTestsJson?: string;
  correctExamplesJson?: string;
  wrongExamplesJson?: string;
  [key: string]: any; 
}

export interface Lobby {
  id: number;
  code: string;
  name: string;
  hostPlayer: string;
  levelName: string;
  maxPlayers: number;
  hasStarted: boolean;
  createdAt: string;
  passwordProtected: boolean;
  lobbyPlayers: string[];
}

export interface LeaderboardEntry {
  levelName: string;
  playerName: string;
  time: number;
  nodeCount: number;
  connectionCount: number;
}

export interface AdminLog {
  id: number;
  actorName: string;
  actorRole: string;
  action: string; // "Create", "Update", "Delete", etc.
  targetEntityType: string;
  targetEntityId: number;
  targetEntityName: string;
  doneAt: string;
}

export interface AuthResponse {
  token: string;
  username: string;
  id: number;
}

export interface ApiError {
  message: string;
  statusCode?: number;
}

export interface HealthStatus {
  status: string;
}

export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

// Editor Types
export interface MachineNode {
  id: number;
  x: number;
  y: number;
  is_start: boolean;
  is_end: boolean;
}

export interface MachineConnection {
  start: number;
  end: number;
  read: string[];
  write: string | null;
  move: string | null; // 'L', 'R', 'S'
  read2?: string[];
  write2?: string | null;
  move2?: string | null;
}

export type SimulationConfig = {
  input: string;
  twoTapes: boolean;
}