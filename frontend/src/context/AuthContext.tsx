import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { api, getStoredToken, setStoredToken } from '../api/client';
import type { LoginResponse, User } from '../types';

interface AuthContextValue {
  user: User | null;
  loading: boolean;
  login: (userName: string, password: string) => Promise<void>;
  logout: () => void;
  hasRole: (...roles: string[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  async function loadMe() {
    try {
      const { data } = await api.get<User>('/api/auth/me');
      setUser(data);
    } catch {
      setStoredToken(null);
      setUser(null);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (getStoredToken()) {
      loadMe();
    } else {
      setLoading(false);
    }
  }, []);

  async function login(userName: string, password: string) {
    const { data } = await api.post<LoginResponse>('/api/auth/login', { userName, password });
    setStoredToken(data.token);
    await loadMe();
  }

  function logout() {
    setStoredToken(null);
    setUser(null);
  }

  function hasRole(...roles: string[]) {
    if (!user) return false;
    return roles.some((r) => user.roles.includes(r));
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, logout, hasRole }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider');
  return ctx;
}
