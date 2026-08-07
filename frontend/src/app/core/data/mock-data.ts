/**
 * @deprecated Import domain types from `core/models/*` instead.
 * Kept as a compatibility barrel so existing imports keep compiling.
 */
export type { ProjectRow, CreateProjectRequest, UpdateProjectRequest } from '../models/project.model';
export type { StageRow, CreateStageRequest, UpdateStageRequest } from '../models/stage.model';
export type { TaskCard, CreateTaskRequest, UpdateTaskRequest } from '../models/task.model';
export type { EmployeeRow, EmployeeStatus } from '../models/employee.model';
