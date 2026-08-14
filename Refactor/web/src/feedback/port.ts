import type { UserNotice, UserNoticeKind, UserNoticePort } from './types';

const queue: UserNotice[] = [];

const noopPort: UserNoticePort = {
  report(notice) {
    queue.push(notice);
  },
};

let port: UserNoticePort = noopPort;

export function bindUserNoticePort(next: UserNoticePort | null): void {
  port = next ?? noopPort;
  if (!next) return;
  const pending = queue.splice(0);
  for (const notice of pending) next.report(notice);
}

export function reportUserNotice(notice: UserNotice): void {
  const message = notice.message.trim();
  if (!message) return;
  port.report({ ...notice, message });
}

export function reportError(message: string): void {
  reportUserNotice({ kind: 'error', message });
}

export function reportSuccess(message: string): void {
  reportUserNotice({ kind: 'success', message });
}

export function reportWarning(message: string): void {
  reportUserNotice({ kind: 'warning', message });
}

export function reportInfo(message: string): void {
  reportUserNotice({ kind: 'info', message });
}

export function reportKind(kind: UserNoticeKind, message: string): void {
  reportUserNotice({ kind, message });
}
