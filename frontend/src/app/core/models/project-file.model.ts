export interface ProjectFileResponse {
  id: number;
  projectId: number;
  projectName: string;
  fileName: string;
  description: string | null;
  /** Stored as `category` in the API — used as file type in the UI. */
  category: string | null;
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
  description?: string | null;
  category?: string | null;
  contentType?: string | null;
  sizeBytes: number;
  url: string;
}

export const FILE_TYPES = ['PDF', 'Word', 'Excel', 'Image', 'Video', 'Audio', 'Archive', 'Text', 'Other'] as const;
export type FileType = (typeof FILE_TYPES)[number];

/** Infer a file type from MIME type and/or file name. */
export function detectFileType(file: File): FileType {
  const mime = (file.type || '').toLowerCase();
  const name = file.name.toLowerCase();
  const ext = name.includes('.') ? name.slice(name.lastIndexOf('.') + 1) : '';

  if (mime.includes('pdf') || ext === 'pdf') return 'PDF';
  if (
    mime.includes('word') ||
    mime.includes('msword') ||
    mime.includes('officedocument.wordprocessing') ||
    ['doc', 'docx', 'rtf', 'odt'].includes(ext)
  ) {
    return 'Word';
  }
  if (
    mime.includes('excel') ||
    mime.includes('spreadsheet') ||
    ['xls', 'xlsx', 'csv', 'ods'].includes(ext)
  ) {
    return 'Excel';
  }
  if (mime.startsWith('image/') || ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp', 'heic'].includes(ext)) {
    return 'Image';
  }
  if (mime.startsWith('video/') || ['mp4', 'mov', 'avi', 'mkv', 'webm'].includes(ext)) return 'Video';
  if (mime.startsWith('audio/') || ['mp3', 'wav', 'aac', 'm4a', 'ogg'].includes(ext)) return 'Audio';
  if (
    mime.includes('zip') ||
    mime.includes('compressed') ||
    mime.includes('x-rar') ||
    mime.includes('x-7z') ||
    ['zip', 'rar', '7z', 'tar', 'gz'].includes(ext)
  ) {
    return 'Archive';
  }
  if (mime.startsWith('text/') || ['txt', 'md', 'json', 'xml', 'log'].includes(ext)) return 'Text';
  return 'Other';
}
