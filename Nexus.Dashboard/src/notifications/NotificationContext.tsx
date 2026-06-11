import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { NotificationHost } from './NotificationHost';
import type { NotificationItem, NotificationVariant } from './types';

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

function createNotificationId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [items, setItems] = useState<NotificationItem[]>([]);
  const timersRef = useRef<Map<string, number>>(new Map());

  const dismiss = useCallback((id: string) => {
    const timer = timersRef.current.get(id);
    if (timer !== undefined) {
      window.clearTimeout(timer);
      timersRef.current.delete(id);
    }
    setItems((current) => current.filter((item) => item.id !== id));
  }, []);

  const notify = useCallback((message: string, options?: NotifyOptions) => {
    const trimmed = message.trim();
    if (!trimmed) return;

    const variant = options?.variant ?? 'info';
    const id = createNotificationId();
    const item: NotificationItem = { id, message: trimmed, variant };

    setItems((current) => [...current, item]);

    const durationMs = options?.durationMs ?? DEFAULT_DURATION_MS[variant];
    const timer = window.setTimeout(() => dismiss(id), durationMs);
    timersRef.current.set(id, timer);
  }, [dismiss]);

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
      <NotificationHost items={items} onDismiss={dismiss} />
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
