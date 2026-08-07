namespace Stratix.Domain.Enums;

public enum UserRole { ADMIN, PROJECT_MANAGER, TEAM_LEADER, EMPLOYEE, EXECUTIVE_VIEWER, ORG_ADMIN, SUPER_ADMIN }
public enum UserStatus { ACTIVE, INACTIVE, SUSPENDED }
public enum ProjectStatus { PLANNED, ACTIVE, ON_HOLD, COMPLETED, CANCELLED }
public enum ProjectPriority { LOW, MEDIUM, HIGH, CRITICAL }
public enum StageStatus { PLANNED, ACTIVE, DONE, ON_HOLD }
public enum TaskStatus { TODO, IN_PROGRESS, REVIEW, DONE, BLOCKED }
public enum TaskPriority { LOW, MEDIUM, HIGH, URGENT }
public enum RiskImpact { LOW, MEDIUM, HIGH }
public enum RiskProbability { LOW, MEDIUM, HIGH }
public enum RiskLevel { LOW, MEDIUM, HIGH, CRITICAL }
public enum RiskStatus { OPEN, MITIGATING, CLOSED }
public enum AuditAction { CREATE, UPDATE, DELETE, STATUS_CHANGE, ASSIGNMENT_CHANGE, PRIORITY_CHANGE }
public enum AuditEntityType { PROJECT, STAGE, TASK, RISK, USER, MILESTONE, REPORT }
public enum HealthClassification { HEALTHY, WARNING, CRITICAL }
public enum DeliveryRisk { LOW, MEDIUM, HIGH }
public enum NotificationType { INFO, SUCCESS, WARNING, ERROR }
public enum OrganizationStatus { ACTIVE, SUSPENDED, CANCELLED }
public enum SubscriptionPlan { FREE, PRO, ENTERPRISE }
public enum SubscriptionStatus { TRIALING, ACTIVE, PAST_DUE, CANCELLED }
public enum ReportType
{
    PROJECTS_PROGRESS,
    TASKS_STATUS,
    EMPLOYEE_PERFORMANCE,
    DELAYED_TASKS,
    KPI_SUMMARY,
    CUSTOM
}
public enum ReportFormat { PDF, EXCEL }

public enum EvaluationPeriodStatus { DRAFT, OPEN, CLOSED }
public enum EmployeeEvaluationStatus { DRAFT, SUBMITTED, IN_REVIEW, APPROVED, REJECTED }
