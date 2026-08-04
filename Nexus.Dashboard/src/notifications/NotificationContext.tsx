import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  type ReactNode,
} from 'react';
import { toast } from 'sonner';
import type { NotificationVariant } from './types';

type NotifyOptions = {
  variant?: NotificationVariant;
  durationMs?: number;
};

type NotificationContextValue = {
  notify: (message: string, options?: NotifyOptions) => void;
  notifyError: (message: string) => void;
  notifySuccess: (message: string) => void;
  dismiss: (id: string) => void;
};

const DEFAULT_DURATION_MS: Record<NotificationVariant, number> = {
  error: 7000,
  success: 5000,
  info: 5000,
};

const NotificationContext = createContext<NotificationContextValue | null>(null);

export function NotificationProvider({ children }: { children: ReactNode }) {
  const dismiss = useCallback((id: string) => {
    toast.dismiss(id);
  }, []);

  const notify = useCallback((message: string, options?: NotifyOptions) => {
    const trimmed = message.trim();
    if (!trimmed) return;

    const variant = options?.variant ?? 'info';
    const duration = options?.durationMs ?? DEFAULT_DURATION_MS[variant];

    if (variant === 'error') {
      toast.error(trimmed, { duration });
      return;
    }
    if (variant === 'success') {
      toast.success(trimmed, { duration });
      return;
    }
    toast.info(trimmed, { duration });
  }, []);

  const notifyError = useCallback((message: string) => {
    notify(message, { variant: 'error' });
  }, [notify]);

  const notifySuccess = useCallback((message: string) => {
    notify(message, { variant: 'success' });
  }, [notify]);

  const value = useMemo<NotificationContextValue>(() => ({
    notify,
    notifyError,
    notifySuccess,
    dismiss,
  }), [notify, notifyError, notifySuccess, dismiss]);

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotifications(): NotificationContextValue {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within NotificationProvider');
  }
  return context;
}
