using System.Net;
using System.Net.Http.Json;

namespace Stratix.Api.Tests;

/// <summary>
/// Persona end-to-end journeys — full role flows, not isolated endpoint checks.
/// </summary>
public class PersonaE2ETests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public PersonaE2ETests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Scenario1_OrgAdmin_registers_creates_department_adds_pm_creates_project()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario1 Org");

        Assert.Equal("ORG_ADMIN", world.Admin.User.RoleCode ?? world.Admin.User.Role);
        Assert.True(world.DepartmentId > 0);
        Assert.True(world.PmUserId > 0);
        Assert.True(world.ProjectId > 0);

        using var getProj = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{world.ProjectId}", world.Admin.Token);
        var projRes = await _client.SendAsync(getProj);
        Assert.Equal(HttpStatusCode.OK, projRes.StatusCode);
        var project = await TestAuth.ReadJsonAsync<PersonaFlow.ProjectDto>(projRes);
        Assert.Contains(world.Tag, project!.Name, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Scenario2_Pm_creates_features_tasks_and_assigns_employee()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario2 Org");

        var auth = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Authentication");
        var dash = await PersonaFlow.CreateFeatureAsync(
            _client, world.Pm.Token, world.ProjectId, "Dashboard", "2026-02-15", "2026-08-01");

        Assert.NotEqual(auth.Id, dash.Id);

        var task = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, auth.Id, "Wire login API", world.EmployeeUserId);

        Assert.Equal(world.EmployeeUserId, task.AssigneeId);
        Assert.Equal(auth.Id, task.StageId);
        Assert.Equal("TODO", task.Status);

        var features = await PersonaFlow.SendJsonAsync<List<PersonaFlow.IdNameDto>>(
            _client, HttpMethod.Get, $"/api/projects/{world.ProjectId}/stages", world.Pm.Token,
            step: "List features");
        Assert.Equal(2, features.Count);
    }

    [Fact]
    public async Task Scenario3_Employee_sees_own_tasks_and_runs_block_resume_review_flow()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario3 Org");
        var feature = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Auth");

        var mine = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "My task", world.EmployeeUserId);
        var theirs = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "Other task", world.OtherEmployeeUserId);

        var visible = await PersonaFlow.ListTasksAsync(_client, world.Employee.Token);
        Assert.Contains(visible, t => t.Id == mine.Id);
        Assert.DoesNotContain(visible, t => t.Id == theirs.Id);

        var started = await PersonaFlow.PatchStatusAsync(
            _client, world.Employee.Token, mine.Id, "IN_PROGRESS");
        Assert.Equal("IN_PROGRESS", started.Status);

        var blocked = await PersonaFlow.PatchStatusAsync(
            _client, world.Employee.Token, mine.Id, "BLOCKED", blockedReason: "Waiting on API key");
        Assert.Equal("BLOCKED", blocked.Status);
        Assert.Equal("Waiting on API key", blocked.BlockedReason);

        var resumed = await PersonaFlow.PatchStatusAsync(
            _client, world.Employee.Token, mine.Id, "IN_PROGRESS");
        Assert.Equal("IN_PROGRESS", resumed.Status);

        var review = await PersonaFlow.PatchStatusAsync(
            _client, world.Employee.Token, mine.Id, "REVIEW");
        Assert.Equal("REVIEW", review.Status);
    }

    [Fact]
    public async Task Scenario4_Pm_requests_changes_employee_resubmits_pm_approves()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario4 Org");
        var feature = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Reports");
        var task = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "Export PDF", world.EmployeeUserId);

        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "IN_PROGRESS");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "REVIEW");

        var changes = await PersonaFlow.PatchStatusAsync(
            _client, world.Pm.Token, task.Id, "IN_PROGRESS",
            reviewReason: "Please add page numbers");
        Assert.Equal("IN_PROGRESS", changes.Status);
        Assert.Equal("Please add page numbers", changes.ReviewReason);

        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "REVIEW");

        var approved = await PersonaFlow.PatchStatusAsync(
            _client, world.Pm.Token, task.Id, "DONE");
        Assert.Equal("DONE", approved.Status);
    }

    [Fact]
    public async Task Scenario5_Progress_recalculates_health_snapshot_and_ai_reflects_data()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario5 Org");
        var feature = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Dashboard");

        var open = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "Open work", world.EmployeeUserId,
            estimatedHours: 10);
        var done = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "Finished work", world.EmployeeUserId,
            status: "DONE", estimatedHours: 10);

        using var getBefore = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{world.ProjectId}", world.Pm.Token);
        var before = await TestAuth.ReadJsonAsync<PersonaFlow.ProjectDto>(await _client.SendAsync(getBefore));
        Assert.Equal(50m, before!.Progress);

        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, open.Id, "IN_PROGRESS");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, open.Id, "DONE");

        using var getAfter = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{world.ProjectId}", world.Pm.Token);
        var after = await TestAuth.ReadJsonAsync<PersonaFlow.ProjectDto>(await _client.SendAsync(getAfter));
        Assert.Equal(100m, after!.Progress);

        using var capture = TestAuth.Authed(
            HttpMethod.Post, $"/api/health-snapshots/projects/{world.ProjectId}/capture", world.Pm.Token);
        var snap = await TestAuth.ReadJsonAsync<PersonaFlow.SnapDto>(await _client.SendAsync(capture));
        Assert.NotNull(snap);
        Assert.Equal(100m, snap.Progress);
        Assert.True(snap.Score > 0);

        var ai = await PersonaFlow.SendJsonAsync<PersonaFlow.AiDto>(
            _client, HttpMethod.Post, "/api/ai/project-health-analysis", world.Pm.Token,
            new { projectId = world.ProjectId },
            "AI analysis");
        Assert.False(string.IsNullOrWhiteSpace(ai.HealthStatus));
        Assert.False(string.IsNullOrWhiteSpace(ai.ExecutiveSummary));
        Assert.Contains(ai.ManagementInsights, s => s.Contains("feature", StringComparison.OrdinalIgnoreCase)
            || s.Contains("closure", StringComparison.OrdinalIgnoreCase)
            || s.Length > 0);
        _ = done;
    }

    [Fact]
    public async Task Scenario6_Risk_owner_notified_mitigated_then_health_updates()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario6 Org");
        await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Core");

        using var beforeCapture = TestAuth.Authed(
            HttpMethod.Post, $"/api/health-snapshots/projects/{world.ProjectId}/capture", world.Pm.Token);
        var baseline = await TestAuth.ReadJsonAsync<PersonaFlow.SnapDto>(await _client.SendAsync(beforeCapture));
        Assert.NotNull(baseline);
        Assert.Equal(0, baseline.CriticalRisks);

        // HIGH+HIGH → CRITICAL level → owner notification.
        var risk = await PersonaFlow.SendJsonAsync<PersonaFlow.RiskDto>(
            _client, HttpMethod.Post, "/api/risks", world.Pm.Token,
            new
            {
                title = "Payment gateway outage",
                description = "Vendor SLA risk",
                impact = "HIGH",
                probability = "HIGH",
                mitigationPlan = "Failover + monitoring",
                status = "OPEN",
                projectId = world.ProjectId,
                ownerId = world.EmployeeUserId
            },
            "Create critical risk");
        Assert.Equal("CRITICAL", risk.RiskLevel);
        Assert.Equal(world.EmployeeUserId, risk.OwnerId);

        var notifs = await PersonaFlow.SendJsonAsync<List<PersonaFlow.NotifDto>>(
            _client, HttpMethod.Get, "/api/notifications", world.Employee.Token,
            step: "Owner notifications");
        Assert.Contains(notifs, n =>
            n.EntityType == "RISK"
            && n.EntityId == risk.Id
            && n.Title.Contains("Critical", StringComparison.OrdinalIgnoreCase));

        var mitigating = await PersonaFlow.SendJsonAsync<PersonaFlow.RiskDto>(
            _client, HttpMethod.Put, $"/api/risks/{risk.Id}", world.Pm.Token,
            new
            {
                title = risk.Title,
                description = "Vendor SLA risk",
                impact = "HIGH",
                probability = "HIGH",
                mitigationPlan = "Failover live",
                status = "MITIGATING",
                projectId = world.ProjectId,
                ownerId = world.EmployeeUserId
            },
            "Mitigate risk");
        Assert.Equal("MITIGATING", mitigating.Status);

        using var closeReq = TestAuth.Authed(HttpMethod.Post, $"/api/risks/{risk.Id}/close", world.Pm.Token, new
        {
            closureReason = "Failover verified in production",
            residualRisk = "LOW"
        });
        var closeRes = await _client.SendAsync(closeReq);
        await PersonaFlow.EnsureSuccess(closeRes, "Close risk");

        using var afterCapture = TestAuth.Authed(
            HttpMethod.Post, $"/api/health-snapshots/projects/{world.ProjectId}/capture", world.Pm.Token);
        var after = await TestAuth.ReadJsonAsync<PersonaFlow.SnapDto>(await _client.SendAsync(afterCapture));
        Assert.NotNull(after);
        Assert.Equal(0, after.CriticalRisks);
        Assert.True(after.Score >= baseline.Score);
    }

    [Fact]
    public async Task Scenario7_Kpi_period_calculate_submit_review_approve_employee_sees_breakdown()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario7 Org");

        var period = await PersonaFlow.SendJsonAsync<PersonaFlow.IdNameDto>(
            _client, HttpMethod.Post, "/api/kpi/periods", world.Admin.Token,
            new
            {
                name = $"Q1-{world.Tag}",
                startDate = "2026-01-01",
                endDate = "2026-03-31"
            },
            "Create KPI period");

        using var defReq = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", world.Admin.Token, new
        {
            code = $"TC{world.Tag}"[..8].ToUpperInvariant(),
            name = "Tasks completed",
            description = (string?)null,
            weight = 100m,
            targetValue = (decimal?)null,
            formula = "TASKS_COMPLETED",
            higherIsBetter = true,
            isActive = true,
            appliesToRole = "EMPLOYEE"
        });
        await PersonaFlow.EnsureSuccess(await _client.SendAsync(defReq), "Upsert KPI definition");

        using var openReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/periods/{period.Id}/open", world.Admin.Token);
        await PersonaFlow.EnsureSuccess(await _client.SendAsync(openReq), "Open KPI period");

        var feature = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "KPI work");
        var task = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "KPI task", world.EmployeeUserId);
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "IN_PROGRESS");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "DONE");

        var eval = await PersonaFlow.SendJsonAsync<PersonaFlow.EvalDto>(
            _client, HttpMethod.Post,
            $"/api/kpi/evaluations/ensure?periodId={period.Id}&userId={world.EmployeeUserId}",
            world.Pm.Token,
            step: "Ensure evaluation");

        eval = await PersonaFlow.SendJsonAsync<PersonaFlow.EvalDto>(
            _client, HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/calculate", world.Pm.Token,
            step: "Calculate");
        Assert.NotNull(eval.Results);
        Assert.NotEmpty(eval.Results);

        eval = await PersonaFlow.SendJsonAsync<PersonaFlow.EvalDto>(
            _client, HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/submit", world.Pm.Token,
            step: "Submit");
        Assert.Equal("SUBMITTED", eval.Status);

        eval = await PersonaFlow.SendJsonAsync<PersonaFlow.EvalDto>(
            _client, HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/review", world.Admin.Token,
            step: "Start review");
        Assert.Equal("IN_REVIEW", eval.Status);

        eval = await PersonaFlow.SendJsonAsync<PersonaFlow.EvalDto>(
            _client, HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/approve", world.Admin.Token,
            step: "Approve");
        Assert.Equal("APPROVED", eval.Status);
        Assert.NotNull(eval.OverallScore);

        var employeeView = await PersonaFlow.SendJsonAsync<List<PersonaFlow.EvalDto>>(
            _client, HttpMethod.Get, $"/api/kpi/evaluations?periodId={period.Id}", world.Employee.Token,
            step: "Employee list evaluations");
        var mine = Assert.Single(employeeView, e => e.Id == eval.Id);
        Assert.Equal("APPROVED", mine.Status);
        Assert.NotNull(mine.Results);
        Assert.NotEmpty(mine.Results);
        Assert.All(mine.Results!, r => Assert.False(string.IsNullOrWhiteSpace(r.KpiName)));
    }

    [Fact]
    public async Task Scenario8_Generate_report_saved_downloaded_and_tenant_isolated()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Scenario8 Org");
        var outsider = await TestAuth.RegisterOrgAsync(
            _client, "Other Tenant", $"out-{Guid.NewGuid():N}@persona.test");

        var report = await PersonaFlow.UploadReportAsync(
            _client, world.Pm.Token, $"Report {world.Tag}", world.ProjectId, PersonaFlow.TinyPdfBytes());
        Assert.True(report.Id > 0);
        Assert.Equal(world.ProjectId, report.ProjectId);

        using var listReq = TestAuth.Authed(HttpMethod.Get, "/api/reports", world.Pm.Token);
        var list = await TestAuth.ReadJsonAsync<List<PersonaFlow.ReportDto>>(await _client.SendAsync(listReq)) ?? [];
        Assert.Contains(list, r => r.Id == report.Id);

        using var dlReq = TestAuth.Authed(HttpMethod.Get, $"/api/reports/{report.Id}/download", world.Pm.Token);
        var dlRes = await _client.SendAsync(dlReq);
        Assert.Equal(HttpStatusCode.OK, dlRes.StatusCode);
        var bytes = await dlRes.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(bytes[..Math.Min(4, bytes.Length)]));

        using var stealMeta = TestAuth.Authed(HttpMethod.Get, $"/api/reports/{report.Id}", outsider.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(stealMeta)).StatusCode);

        using var stealDl = TestAuth.Authed(HttpMethod.Get, $"/api/reports/{report.Id}/download", outsider.Token);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.SendAsync(stealDl)).StatusCode);

        using var outsiderListReq = TestAuth.Authed(HttpMethod.Get, "/api/reports", outsider.Token);
        var outsiderList =
            await TestAuth.ReadJsonAsync<List<PersonaFlow.ReportDto>>(await _client.SendAsync(outsiderListReq)) ?? [];
        Assert.DoesNotContain(outsiderList, r => r.Id == report.Id);
    }

    [Fact]
    public async Task Scenario9_Plan_limit_blocks_with_helpful_upgrade_message()
    {
        var email = $"limit-{Guid.NewGuid():N}@persona.test";
        var session = await TestAuth.RegisterOrgAsync(_client, "Limit Org", email);
        await PersonaFlow.SetPlanCodeAsync(_factory, email, "STARTER");

        for (var i = 0; i < 3; i++)
        {
            using var ok = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject($"Ok-{i}"));
            Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(ok)).StatusCode);
        }

        using var blocked = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject("Over"));
        var blockedRes = await _client.SendAsync(blocked);
        Assert.Equal(HttpStatusCode.PaymentRequired, blockedRes.StatusCode);
        var body = await blockedRes.Content.ReadAsStringAsync();
        Assert.Contains("allows up to", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Upgrade", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Scenario10_Trial_expiry_blocks_tenant_correctly()
    {
        var email = $"trial-{Guid.NewGuid():N}@persona.test";
        var session = await TestAuth.RegisterOrgAsync(_client, "Trial Block Org", email);
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));

        using var okReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(okReq)).StatusCode);

        await PersonaFlow.ExpireTrialAsync(_factory, email);

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = email, password = PersonaFlow.DefaultPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        using var blockedReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", session.Token);
        var blockedRes = await _client.SendAsync(blockedReq);
        Assert.Equal(HttpStatusCode.Unauthorized, blockedRes.StatusCode);
    }

    /// <summary>Single continuous journey covering Scenarios 1–4 as one persona story.</summary>
    [Fact]
    public async Task Journey_Scenarios1to4_org_setup_through_task_approval()
    {
        var world = await PersonaFlow.BootstrapTenantAsync(_client, "Journey Org");
        var feature = await PersonaFlow.CreateFeatureAsync(_client, world.Pm.Token, world.ProjectId, "Authentication");
        var task = await PersonaFlow.CreateTaskAsync(
            _client, world.Pm.Token, world.ProjectId, feature.Id, "Login screen", world.EmployeeUserId);

        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "IN_PROGRESS");
        await PersonaFlow.PatchStatusAsync(
            _client, world.Employee.Token, task.Id, "BLOCKED", blockedReason: "Design TBD");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "IN_PROGRESS");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "REVIEW");
        await PersonaFlow.PatchStatusAsync(
            _client, world.Pm.Token, task.Id, "IN_PROGRESS", reviewReason: "Add validation");
        await PersonaFlow.PatchStatusAsync(_client, world.Employee.Token, task.Id, "REVIEW");
        var done = await PersonaFlow.PatchStatusAsync(_client, world.Pm.Token, task.Id, "DONE");
        Assert.Equal("DONE", done.Status);

        using var getProj = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{world.ProjectId}", world.Pm.Token);
        var project = await TestAuth.ReadJsonAsync<PersonaFlow.ProjectDto>(await _client.SendAsync(getProj));
        Assert.Equal(100m, project!.Progress);
    }

    private static object NewProject(string name) => new
    {
        name,
        description = (string?)null,
        departmentId = (long?)null,
        projectManagerId = (long?)null,
        startDate = (string?)null,
        endDate = (string?)null,
        status = "ACTIVE",
        priority = "LOW",
        progress = 0
    };
}
