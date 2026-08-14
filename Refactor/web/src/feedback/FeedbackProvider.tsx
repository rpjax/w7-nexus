import { useEffect, type ReactNode } from 'react';
import { toast } from 'sonner';
import { Toaster } from '@/components/ui/sonner';
import { bindUserNoticePort } from './port';
import type { UserNotice, UserNoticePort } from './types';

const sonnerPort: UserNoticePort = {
  report(notice: UserNotice) {
    switch (notice.kind) {
      case 'success':
        toast.success(notice.message);
        return;
      case 'warning':
        toast.warning(notice.message);
        return;
      case 'info':
        toast.info(notice.message);
        return;
      default:
        toast.error(notice.message);
    }
  },
};

function noticeFromUnknown(value: unknown): string | null {
  if (typeof value === 'string' && value.trim()) return value;
  if (value instanceof Error && value.message.trim()) {
    if (value.name === 'AbortError') return null;
    return value.message;
  }
  if (value && typeof value === 'object' && 'message' in value) {
    const message = (value as { message?: unknown }).message;
    if (typeof message === 'string' && message.trim()) return message;
  }
  return null;
}

export function FeedbackProvider({ children }: { children: ReactNode }) {
  useEffect(() => {
    bindUserNoticePort(sonnerPort);
    return () => bindUserNoticePort(null);
  }, []);

  useEffect(() => {
    const onRejection = (event: PromiseRejectionEvent) => {
      let message = noticeFromUnknown(event.reason);
      if (!message) return;
      if (/failed to fetch|networkerror|load failed/i.test(message)) {
        message = 'Não foi possível conectar à API. Verifique a rede e se o serviço está no ar.';
      }
      sonnerPort.report({ kind: 'error', message });
    };

    window.addEventListener('unhandledrejection', onRejection);
    return () => {
      window.removeEventListener('unhandledrejection', onRejection);
    };
  }, []);

  return (
    <>
      {children}
      <Toaster richColors closeButton position="top-right" />
    </>
  );
}
