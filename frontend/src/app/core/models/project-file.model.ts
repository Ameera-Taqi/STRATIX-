export interface ProjectFileResponse {
  id: number;
  projectId: number;
  projectName: string;
  fileName: string;
  contentType: string | null;
  sizeBytes: number;
  url: string;
  uploadedById: number;
  uploadedByName: string;
  createdAt: string;
}

export interface CreateProjectFileRequest {
  projectId: number;
  fileName: string;
  contentType?: string | null;
  sizeBytes: number;
  url: string;
}
