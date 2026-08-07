export type NotificationApiType = 'INFO' | 'SUCCESS' | 'WARNING' | 'ERROR';
export type NotificationEntityType = 'TASK' | 'RISK' | 'PROJECT' | 'STAGE' | 'EVALUATION' | string;

export interface NotificationApiResponse {
  id: number;
  userId: number;
  title: string;
  message: string | null;
  type: NotificationApiType | string;
  isRead: boolean;
  link: string | null;
  createdAt: string;
  actorName?: string | null;
  projectName?: string | null;
  entityType?: NotificationEntityType | null;
  entityId?: number | null;
  entityLabel?: string | null;
}

export interface CreateNotificationRequest {
  userId: number;
  title: string;
  message?: string | null;
  type?: NotificationApiType | null;
  link?: string | null;
  actorName?: string | null;
  projectName?: string | null;
  entityType?: NotificationEntityType | null;
  entityId?: number | null;
  entityLabel?: string | null;
}
