import React, { createContext, useContext, useState, useEffect } from 'react';
import { ConnectionStatus } from '../types';
import { signalRService } from '../services/signalr';

interface SignalRContextType {
  status: ConnectionStatus;
  subscribeToExecution: (id: string) => Promise<void>;
  unsubscribeFromExecution: (id: string) => Promise<void>;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

export const SignalRProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [status, setStatus] = useState<ConnectionStatus>(signalRService.status);

  useEffect(() => {
    const unsubscribe = signalRService.onStatusChange((newStatus) => {
      setStatus(newStatus);
    });
    return () => unsubscribe();
  }, []);

  return (
    <SignalRContext.Provider
      value={{
        status,
        subscribeToExecution: (id) => signalRService.subscribeToExecution(id),
        unsubscribeFromExecution: (id) => signalRService.unsubscribeFromExecution(id),
      }}
    >
      {children}
    </SignalRContext.Provider>
  );
};

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (!context) {
    throw new Error('useSignalR must be used within a SignalRProvider');
  }
  return context;
};
