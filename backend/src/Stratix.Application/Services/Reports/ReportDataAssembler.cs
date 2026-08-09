using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Reports;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;
using TaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.Services.Reports;

/// <summary>Loads tenant-scoped authoritative data and shapes it into report sections.</summary>
public sealed class ReportDataAssembler
{
    private readonly IApplicationDbContext _db;

    public ReportDataAssembler(IApplicationDbContext db) => _db = db;

    public async Task<(ReportDocumentModel Model, string ResolvedTitle, string? ProjectName)> AssembleAsync(
        GenerateReportRequest request,
        ReportType reportType,
        long organizationId,
        CancellationToken ct)
    {
        string? projectName = null;
        if (request.ProjectId is long projectId)
        {
            projectName = await _db.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(ct);
        }

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? DefaultTitle(reportType, projectName, request.DateFrom, request.DateTo)
            : request.Title.Trim();

        var sections = reportType switch
        {
            ReportType.PROJECTS_PROGRESS => await BuildProjectsProgressAsync(request, organizationId, ct),
            ReportType.TASKS_STATUS => await BuildTasksStatusAsync(request, organizationId, delayedOnly: false, ct),
            ReportType.DELAYED_TASKS => await BuildTasksStatusAsync(request, organizationId, delayedOnly: true, ct),
            ReportType.EMPLOYEE_PERFORMANCE => await BuildEmployeePerformanceAsync(request, organizationId, ct),
            ReportType.KPI_SUMMARY => await BuildKpiSummaryAsync(request, organizationId, ct),
            _ => await BuildProjectsProgressAsync(request, organizationId, ct)
        };

        var model = new ReportDocumentModel
        {
            Title = title,
            ReportType = reportType.ToString(),
            Format = request.Format,
            ProjectName = projectName,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            GeneratedAt = DateTimeOffset.UtcNow,
            Sections = sections
        };

        return (model, title, projectName);
    }

    private async Task<IReadOnlyList<ReportSection>> BuildProjectsProgressAsync(
        GenerateReportRequest request,
        long organizationId,
        CancellationToken ct)
    {
        var projectsQuery = _db.Projects.AsQueryable();
        if (request.ProjectId is long pid)
            projectsQuery = projectsQuery.Where(p => p.Id == pid);
        if (request.DepartmentId is long deptId)
            projectsQuery = projectsQuery.Where(p => p.DepartmentId == deptId);

        var projects = await projectsQuery
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Status = p.Status.ToString(),
                Priority = p.Priority.ToString(),
                p.Progress,
                p.StartDate,
                p.EndDate,
                Manager = p.ProjectManager != null ? p.ProjectManager.Name : null,
                Department = p.Department != null ? p.Department.Name : null
            })
            .ToListAsync(ct);

        var projectIds = projects.Select(p => p.Id).ToList();
        var healthRows = await _db.ProjectHealthSnapshots
            .Where(h => projectIds.Contains(h.ProjectId))
            .OrderByDescending(h => h.CapturedAt)
            .ToListAsync(ct);
        var healthByProject = healthRows
            .GroupBy(h => h.ProjectId)
            .ToDictionary(g => g.Key, g => g.First());

        var criticalRisks = await _db.ProjectRisks
            .Where(r => projectIds.Contains(r.ProjectId) && r.RiskLevel == RiskLevel.CRITICAL && r.Status != RiskStatus.CLOSED)
            .GroupBy(r => r.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count, ct);

        var rows = projects.Select(p =>
        {
            healthByProject.TryGetValue(p.Id, out var health);
            criticalRisks.TryGetValue(p.Id, out var riskCount);
            return (IReadOnlyList<string>)new[]
            {
                p.Name,
                p.Status,
                $"{p.Progress:0.#}%",
                health != null ? $"{health.Score:0.#}" : "—",
                health?.Status.ToString() ?? "—",
                riskCount.ToString(),
                p.Manager ?? "—",
                p.Department ?? "—"
            };
        }).ToList();

        return
        [
            new ReportSection
            {
                Heading = "Projects progress",
                SummaryLines =
                [
                    $"Projects: {projects.Count}",
                    $"Critical open risks (selected projects): {criticalRisks.Values.Sum()}"
                ],
                Columns = ["Project", "Status", "Progress", "Health", "Health status", "Critical risks", "Manager", "Department"],
                Rows = rows
            }
        ];
    }

    private async Task<IReadOnlyList<ReportSection>> BuildTasksStatusAsync(
        GenerateReportRequest request,
        long organizationId,
        bool delayedOnly,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Tasks.AsQueryable();

        if (request.ProjectId is long pid)
            query = query.Where(t => t.ProjectId == pid);
        if (request.DepartmentId is long deptId)
            query = query.Where(t => t.Project.DepartmentId == deptId);
        if (request.EmployeeId is long empId)
            query = query.Where(t => t.AssigneeId == empId);
        if (request.DateFrom is DateOnly from)
            query = query.Where(t => t.DueDate == null || t.DueDate >= from);
        if (request.DateTo is DateOnly to)
            query = query.Where(t => t.DueDate == null || t.DueDate <= to);

        if (delayedOnly)
            query = query.Where(t => t.Status != TaskStatus.DONE && t.DueDate != null && t.DueDate < today);

        var tasks = await query
            .OrderBy(t => t.DueDate)
            .ThenBy(t => t.Title)
            .Take(2000)
            .Select(t => new
            {
                t.Title,
                Status = t.Status.ToString(),
                Priority = t.Priority.ToString(),
                Project = t.Project.Name,
                Assignee = t.Assignee != null ? t.Assignee.Name : null,
                t.DueDate,
                t.Progress
            })
            .ToListAsync(ct);

        var rows = tasks.Select(t => (IReadOnlyList<string>)new[]
        {
            t.Title,
            t.Project,
            t.Status,
            t.Priority,
            t.Assignee ?? "—",
            t.DueDate?.ToString("yyyy-MM-dd") ?? "—",
            $"{t.Progress:0.#}%"
        }).ToList();

        var byStatus = tasks.GroupBy(t => t.Status)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        return
        [
            new ReportSection
            {
                Heading = delayedOnly ? "Delayed tasks" : "Tasks status",
                SummaryLines = byStatus.Count > 0 ? byStatus : ["No tasks matched the filters."],
                Columns = ["Task", "Project", "Status", "Priority", "Assignee", "Due date", "Progress"],
                Rows = rows
            }
        ];
    }

    private async Task<IReadOnlyList<ReportSection>> BuildEmployeePerformanceAsync(
        GenerateReportRequest request,
        long organizationId,
        CancellationToken ct)
    {
        var usersQuery = _db.Users.Where(u => u.Status == UserStatus.ACTIVE);
        if (request.DepartmentId is long deptId)
            usersQuery = usersQuery.Where(u => u.DepartmentId == deptId);
        if (request.EmployeeId is long empId)
            usersQuery = usersQuery.Where(u => u.Id == empId);

        var users = await usersQuery
            .OrderBy(u => u.Name)
            .Select(u => new
            {
                u.Id,
                u.Name,
                Role = u.Role.ToString(),
                Department = u.Department != null ? u.Department.Name : null
            })
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var taskQuery = _db.Tasks.Where(t => t.AssigneeId != null && userIds.Contains(t.AssigneeId.Value));
        if (request.ProjectId is long pid)
            taskQuery = taskQuery.Where(t => t.ProjectId == pid);
        if (request.DateFrom is DateOnly from)
            taskQuery = taskQuery.Where(t => t.DueDate == null || t.DueDate >= from);
        if (request.DateTo is DateOnly to)
            taskQuery = taskQuery.Where(t => t.DueDate == null || t.DueDate <= to);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stats = await taskQuery
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g => new
            {
                AssigneeId = g.Key,
                Total = g.Count(),
                Done = g.Count(t => t.Status == TaskStatus.DONE),
                Delayed = g.Count(t => t.Status != TaskStatus.DONE && t.DueDate != null && t.DueDate < today),
                Blocked = g.Count(t => t.Status == TaskStatus.BLOCKED)
            })
            .ToDictionaryAsync(x => x.AssigneeId, ct);

        var rows = users.Select(u =>
        {
            stats.TryGetValue(u.Id, out var s);
            var total = s?.Total ?? 0;
            var done = s?.Done ?? 0;
            var rate = total == 0 ? "—" : $"{(100m * done / total):0.#}%";
            return (IReadOnlyList<string>)new[]
            {
                u.Name,
                u.Department ?? "—",
                u.Role,
                total.ToString(),
                done.ToString(),
                (s?.Delayed ?? 0).ToString(),
                (s?.Blocked ?? 0).ToString(),
                rate
            };
        }).ToList();

        return
        [
            new ReportSection
            {
                Heading = "Employee performance",
                SummaryLines = [$"Employees: {users.Count}"],
                Columns = ["Employee", "Department", "Role", "Tasks", "Done", "Delayed", "Blocked", "Completion"],
                Rows = rows
            }
        ];
    }

    private async Task<IReadOnlyList<ReportSection>> BuildKpiSummaryAsync(
        GenerateReportRequest request,
        long organizationId,
        CancellationToken ct)
    {
        var periodsQuery = _db.EvaluationPeriods.AsQueryable();
        if (request.DateFrom is DateOnly from)
            periodsQuery = periodsQuery.Where(p => p.EndDate >= from);
        if (request.DateTo is DateOnly to)
            periodsQuery = periodsQuery.Where(p => p.StartDate <= to);

        var periods = await periodsQuery
            .OrderByDescending(p => p.StartDate)
            .Take(12)
            .Select(p => new { p.Id, p.Name, Status = p.Status.ToString(), p.StartDate, p.EndDate })
            .ToListAsync(ct);

        var periodIds = periods.Select(p => p.Id).ToList();

        var resultsQuery = _db.EmployeeKpiResults
            .Where(r => periodIds.Contains(r.Evaluation!.PeriodId));
        if (request.EmployeeId is long empId)
            resultsQuery = resultsQuery.Where(r => r.Evaluation!.UserId == empId);
        if (request.DepartmentId is long deptId)
            resultsQuery = resultsQuery.Where(r => r.Evaluation!.User!.DepartmentId == deptId);

        var results = await resultsQuery
            .OrderByDescending(r => r.UpdatedAt)
            .Take(2000)
            .Select(r => new
            {
                Period = r.Evaluation!.Period!.Name,
                Employee = r.Evaluation.User!.Name,
                Kpi = r.SnapshotName != "" ? r.SnapshotName : (r.KpiDefinition != null ? r.KpiDefinition.Name : r.SnapshotCode),
                r.Score,
                Weight = r.SnapshotWeight,
                Status = r.Evaluation.Status.ToString()
            })
            .ToListAsync(ct);

        var periodRows = periods.Select(p => (IReadOnlyList<string>)new[]
        {
            p.Name,
            p.Status,
            p.StartDate.ToString("yyyy-MM-dd"),
            p.EndDate.ToString("yyyy-MM-dd")
        }).ToList();

        var resultRows = results.Select(r => (IReadOnlyList<string>)new[]
        {
            r.Period,
            r.Employee,
            r.Kpi,
            r.Score.ToString("0.##"),
            r.Weight.ToString("0.##"),
            r.Status
        }).ToList();

        return
        [
            new ReportSection
            {
                Heading = "Evaluation periods",
                SummaryLines = [$"Periods in range: {periods.Count}"],
                Columns = ["Period", "Status", "Start", "End"],
                Rows = periodRows
            },
            new ReportSection
            {
                Heading = "KPI results",
                SummaryLines = [$"Result rows: {results.Count}"],
                Columns = ["Period", "Employee", "KPI", "Score", "Weight", "Evaluation status"],
                Rows = resultRows
            }
        ];
    }

    private static string DefaultTitle(ReportType type, string? projectName, DateOnly? from, DateOnly? to)
    {
        var typeLabel = type switch
        {
            ReportType.PROJECTS_PROGRESS => "Projects progress",
            ReportType.TASKS_STATUS => "Tasks status",
            ReportType.EMPLOYEE_PERFORMANCE => "Employee performance",
            ReportType.DELAYED_TASKS => "Delayed tasks",
            ReportType.KPI_SUMMARY => "KPI summary",
            _ => "Custom report"
        };
        var scope = string.IsNullOrWhiteSpace(projectName) ? "All projects" : projectName;
        var period = from is null && to is null
            ? DateTime.UtcNow.ToString("yyyy-MM")
            : $"{from?.ToString("yyyy-MM-dd") ?? "…"} to {to?.ToString("yyyy-MM-dd") ?? "…"}";
        return $"{typeLabel} — {scope} ({period})";
    }
}
