export type NotificationVariant = 'error' | 'success' | 'info';

export type NotificationItem = {
  id: string;
  message: string;
  variant: NotificationVariant;
};
