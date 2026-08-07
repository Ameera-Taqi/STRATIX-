export type ReportTypeCode =
  | 'PROJECTS_PROGRESS'
  | 'TASKS_STATUS'
  | 'EMPLOYEE_PERFORMANCE'
  | 'DELAYED_TASKS'
  | 'KPI_SUMMARY'
  | 'CUSTOM';

export type ReportFormatCode = 'PDF' | 'EXCEL';

export interface ReportResponse {
  id: number;
  title: string;
  reportType: ReportTypeCode | string;
  format: ReportFormatCode | string;
  projectId: number | null;
  projectName: string | null;
  departmentId: number | null;
  departmentName: string | null;
  employeeId: number | null;
  employeeName: string | null;
  dateFrom: string | null;
  dateTo: string | null;
  fileName: string;
  contentType: string | null;
  sizeBytes: number;
  downloadUrl: string;
  generatedById: number;
  generatedByName: string;
  createdAt: string;
}

export interface CreateReportPayload {
  title: string;
  reportType: ReportTypeCode | string;
  format: ReportFormatCode | string;
  projectId?: number | null;
  departmentId?: number | null;
  employeeId?: number | null;
  dateFrom?: string | null;
  dateTo?: string | null;
}
