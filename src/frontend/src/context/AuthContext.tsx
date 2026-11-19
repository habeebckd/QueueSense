import React, { createContext, useContext, useState, useEffect } from 'react';
import { User, AuthResponse } from '../types';
import { api } from '../services/api';
import { signalRService } from '../services/signalr';

interface AuthContextType {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: { email: string; password: string }) => Promise<void>;
  register: (userData: { name: string; email: string; password: string }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(localStorage.getItem('jobflow_token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    async function loadUser() {
      const storedToken = localStorage.getItem('jobflow_token');
      if (storedToken) {
        try {
          const profile = await api.auth.me();
          setUser(profile);
          signalRService.start();
        } catch {
          logout();
        }
      }
      setIsLoading(false);
    }
    loadUser();
  }, []);

  const handleAuthSuccess = (data: AuthResponse) => {
    localStorage.setItem('jobflow_token', data.token);
    setToken(data.token);
    setUser({
      userId: data.userId,
      name: data.name,
      email: data.email,
      role: data.role,
    });
    signalRService.start();
  };

  const login = async (credentials: { email: string; password: string }) => {
    const data = await api.auth.login(credentials);
    handleAuthSuccess(data);
  };

  const register = async (userData: { name: string; email: string; password: string }) => {
    const data = await api.auth.register(userData);
    handleAuthSuccess(data);
  };

  const logout = () => {
    localStorage.removeItem('jobflow_token');
    setToken(null);
    setUser(null);
    signalRService.stop();
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isAuthenticated: !!token,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
