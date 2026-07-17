export type NotificationApiType = 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR';

export interface NotificationApiResponse {
  id: number;
  userId: number;
  title: string;
  message: string | null;
  type: NotificationApiType | string;
  isRead: boolean;
  link: string | null;
  createdAt: string;
}

export interface CreateNotificationRequest {
  userId: number;
  title: string;
  message?: string | null;
  type?: NotificationApiType | null;
  link?: string | null;
}
