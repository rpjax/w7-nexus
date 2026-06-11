import type { NotificationItem } from './types';

type NotificationHostProps = {
  items: NotificationItem[];
  onDismiss: (id: string) => void;
};

export function NotificationHost({ items, onDismiss }: NotificationHostProps) {
  if (items.length === 0) return null;

  return (
    <div className="notification-host" aria-live="polite" aria-relevant="additions">
      {items.map((item) => (
        <div
          key={item.id}
          className={`notification notification-${item.variant}`}
          role={item.variant === 'error' ? 'alert' : 'status'}
        >
          <p className="notification-message">{item.message}</p>
          <button
            type="button"
            className="notification-dismiss"
            aria-label="Fechar notificação"
            onClick={() => onDismiss(item.id)}
          >
            ×
          </button>
        </div>
      ))}
    </div>
  );
}
