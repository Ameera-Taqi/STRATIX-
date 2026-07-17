export interface TaskCommentResponse {
  id: number;
  taskId: number;
  userId: number;
  userName: string;
  comment: string;
  createdAt: string;
}

export interface CreateTaskCommentRequest {
  taskId: number;
  comment: string;
}
