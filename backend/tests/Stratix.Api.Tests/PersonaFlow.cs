using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Stratix.Api.Tests;

/// <summary>
/// Shared helpers for persona-style E2E journeys (ORG_ADMIN → PM → Employee),
/// not isolated endpoint smoke checks.
/// </summary>
internal static class PersonaFlow
{
    internal const string DefaultPassword = "Secret123!";

    internal static async Task EnsureSuccess(HttpResponseMessage response, string step)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{step} failed ({(int)response.StatusCode}): {body}");
    }

    internal static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string token,
        object? jsonBody = null)
    {
        using var req = TestAuth.Authed(method, url, token, jsonBody);
        return await client.SendAsync(req);
    }

    internal static async Task<T> SendJsonAsync<T>(
        HttpClient client,
        HttpMethod method,
        string url,
        string token,
        object? jsonBody = null,
        string? step = null)
    {
        var res = await SendAsync(client, method, url, token, jsonBody);
        await EnsureSuccess(res, step ?? $"{method} {url}");
        var value = await TestAuth.ReadJsonAsync<T>(res);
        return value ?? throw new InvalidOperationException($"{step ?? url} returned empty body.");
    }

    /// <summary>
    /// Scenario 1 foundation: register ORG_ADMIN → department → PM + Employee → project.
    /// </summary>
    internal static async Task<PersonaWorld> BootstrapTenantAsync(
        HttpClient client,
        string? orgLabel = null)
    {
        var tag = Guid.NewGuid().ToString("N")[..10];
        var orgName = orgLabel ?? $"Persona {tag}";
        var adminEmail = $"admin-{tag}@persona.test";
        var pmEmail = $"pm-{tag}@persona.test";
        var empEmail = $"emp-{tag}@persona.test";
        var otherEmail = $"other-{tag}@persona.test";

        var admin = await TestAuth.RegisterOrgAsync(client, orgName, adminEmail, DefaultPassword);

        var dept = await SendJsonAsync<IdNameDto>(
            client, HttpMethod.Post, "/api/departments", admin.Token,
            new { name = $"Delivery {tag}", description = "Persona E2E department" },
            "Create department");

        var pm = await SendJsonAsync<UserDto>(
            client, HttpMethod.Post, "/api/users", admin.Token,
            new
            {
                name = $"PM {tag}",
                email = pmEmail,
                password = DefaultPassword,
                role = "PROJECT_MANAGER",
                jobTitle = "Project Manager",
                status = "ACTIVE",
                departmentId = dept.Id
            },
            "Add PM");

        var employee = await SendJsonAsync<UserDto>(
            client, HttpMethod.Post, "/api/users", admin.Token,
            new
            {
                name = $"Employee {tag}",
                email = empEmail,
                password = DefaultPassword,
                role = "EMPLOYEE",
                jobTitle = "Engineer",
                status = "ACTIVE",
                departmentId = dept.Id
            },
            "Add employee");

        var other = await SendJsonAsync<UserDto>(
            client, HttpMethod.Post, "/api/users", admin.Token,
            new
            {
                name = $"Other {tag}",
                email = otherEmail,
                password = DefaultPassword,
                role = "EMPLOYEE",
                jobTitle = "Engineer",
                status = "ACTIVE",
                departmentId = dept.Id
            },
            "Add other employee");

        var project = await SendJsonAsync<ProjectDto>(
            client, HttpMethod.Post, "/api/projects", admin.Token,
            new
            {
                name = $"Project {tag}",
                description = "Persona E2E project",
                departmentId = dept.Id,
                projectManagerId = pm.Id,
                startDate = "2026-01-01",
                endDate = "2026-12-31",
                status = "ACTIVE",
                priority = "HIGH",
                progress = 0
            },
            "Create project");

        var pmSession = await TestAuth.LoginAsync(client, pmEmail, DefaultPassword);
        var empSession = await TestAuth.LoginAsync(client, empEmail, DefaultPassword);
        var otherSession = await TestAuth.LoginAsync(client, otherEmail, DefaultPassword);

        return new PersonaWorld(
            Tag: tag,
            Admin: admin,
            Pm: pmSession,
            Employee: empSession,
            OtherEmployee: otherSession,
            DepartmentId: dept.Id,
            ProjectId: project.Id,
            PmUserId: pm.Id,
            EmployeeUserId: employee.Id,
            OtherEmployeeUserId: other.Id,
            AdminEmail: adminEmail,
            PmEmail: pmEmail,
            EmployeeEmail: empEmail);
    }

    internal static async Task<IdNameDto> CreateFeatureAsync(
        HttpClient client,
        string token,
        long projectId,
        string name,
        string? start = "2026-02-01",
        string? end = "2026-06-30")
    {
        return await SendJsonAsync<IdNameDto>(
            client, HttpMethod.Post, $"/api/projects/{projectId}/stages", token,
            new
            {
                name,
                description = $"{name} feature",
                startDate = start,
                endDate = end,
                status = "ACTIVE",
                progress = (decimal?)null,
                orderNumber = (int?)null
            },
            $"Create feature {name}");
    }

    internal static async Task<TaskDto> CreateTaskAsync(
        HttpClient client,
        string token,
        long projectId,
        long? featureId,
        string title,
        long? assigneeId,
        string status = "TODO",
        decimal estimatedHours = 8,
        string? dueDate = "2026-08-01")
    {
        return await SendJsonAsync<TaskDto>(
            client, HttpMethod.Post, "/api/tasks", token,
            new
            {
                projectId,
                stageId = featureId,
                title,
                description = title,
                status,
                priority = "MEDIUM",
                assigneeId,
                startDate = "2026-03-01",
                dueDate,
                estimatedHours,
                actualHours = (decimal?)null
            },
            $"Create task {title}");
    }

    internal static async Task<TaskDto> PatchStatusAsync(
        HttpClient client,
        string token,
        long taskId,
        string status,
        string? blockedReason = null,
        string? reopenReason = null,
        string? reviewReason = null)
    {
        return await SendJsonAsync<TaskDto>(
            client, HttpMethod.Patch, $"/api/tasks/{taskId}/status", token,
            new { status, blockedReason, reopenReason, reviewReason },
            $"Task {taskId} → {status}");
    }

    internal static async Task<List<TaskDto>> ListTasksAsync(HttpClient client, string token, long? projectId = null)
    {
        var url = projectId is null ? "/api/tasks" : $"/api/tasks?projectId={projectId}";
        return await SendJsonAsync<List<TaskDto>>(client, HttpMethod.Get, url, token, null, "List tasks")
               ?? [];
    }

    internal static async Task ExpireTrialAsync(StratixApiFactory factory, string adminEmail)
    {
        await TestAuth.WithDbAsync(factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == adminEmail);
            var sub = await db.SubscriptionSet.FirstAsync(s => s.OrganizationId == user.OrganizationId);
            sub.Status = Stratix.Domain.Enums.SubscriptionStatus.TRIALING;
            sub.TrialEndsAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        });
    }

    internal static async Task SetPlanCodeAsync(StratixApiFactory factory, string adminEmail, string planCode)
    {
        await TestAuth.WithDbAsync(factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == adminEmail);
            var sub = await db.SubscriptionSet.FirstAsync(s => s.OrganizationId == user.OrganizationId);
            sub.PlanCode = planCode;
        });
    }

    internal static async Task<ReportDto> GenerateReportAsync(
        HttpClient client,
        string token,
        string reportType,
        string format,
        long? projectId,
        string? dateFrom = null,
        string? dateTo = null,
        string? title = null)
    {
        var body = new
        {
            title,
            reportType,
            format,
            projectId,
            dateFrom,
            dateTo
        };
        using var req = TestAuth.Authed(HttpMethod.Post, "/api/reports/generate", token, body);
        var res = await client.SendAsync(req);
        await EnsureSuccess(res, "Generate report");
        return (await TestAuth.ReadJsonAsync<ReportDto>(res))!;
    }

    internal static async Task<ReportDto> UploadReportAsync(
        HttpClient client,
        string token,
        string title,
        long? projectId,
        byte[] bytes,
        string fileName = "persona-report.pdf")
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        content.Add(new StringContent("CUSTOM"), "ReportType");
        content.Add(new StringContent("PDF"), "Format");
        if (projectId is not null)
            content.Add(new StringContent(projectId.Value.ToString()), "ProjectId");

        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "file", fileName);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/reports")
        {
            Content = content
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.SendAsync(req);
        await EnsureSuccess(res, "Upload report");
        return (await TestAuth.ReadJsonAsync<ReportDto>(res))!;
    }

    // Minimal valid-ish PDF bytes for signature checks (tests often accept octet streams).
    internal static byte[] TinyPdfBytes() =>
        Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");

    internal sealed record PersonaWorld(
        string Tag,
        AuthSession Admin,
        AuthSession Pm,
        AuthSession Employee,
        AuthSession OtherEmployee,
        long DepartmentId,
        long ProjectId,
        long PmUserId,
        long EmployeeUserId,
        long OtherEmployeeUserId,
        string AdminEmail,
        string PmEmail,
        string EmployeeEmail);

    internal sealed record IdNameDto(long Id, string? Name);
    internal sealed record UserDto(long Id, string Name, string Email, string Role);
    internal sealed record ProjectDto(long Id, string Name, decimal Progress, string? Status);
    internal sealed record TaskDto(
        long Id,
        long ProjectId,
        long? StageId,
        string Title,
        string Status,
        long? AssigneeId,
        string? BlockedReason,
        string? ReviewReason);
    internal sealed record SnapDto(
        long Id,
        decimal Score,
        string Status,
        decimal Progress,
        int OnTimeTasks,
        int DelayedTasks,
        int CriticalRisks);
    internal sealed record AiDto(
        string HealthStatus,
        string DeliveryRisk,
        string ExecutiveSummary,
        List<string> MainConcerns,
        List<string> Recommendations,
        List<string> ManagementInsights);
    internal sealed record NotifDto(long Id, string Title, string? Message, long? EntityId, string? EntityType);
    internal sealed record RiskDto(long Id, string Title, string Status, string RiskLevel, long OwnerId);
    internal sealed record EvalDto(
        long Id,
        string Status,
        decimal? OverallScore,
        List<EvalResultDto>? Results);
    internal sealed record EvalResultDto(
        long Id,
        string KpiCode,
        string KpiName,
        decimal SnapshotWeight,
        decimal Score);
    internal sealed record ReportDto(long Id, string Title, string DownloadUrl, long? ProjectId);
}
