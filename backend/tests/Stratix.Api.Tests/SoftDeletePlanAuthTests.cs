using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Stratix.Api.Tests;

public class SoftDeleteTests : IClassFixture<StratixApiFactory>
{
    private readonly HttpClient _client;

    public SoftDeleteTests(StratixApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Soft_deleted_project_is_hidden_from_get_and_list()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");

        using var createReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", admin.Token, new
        {
            name = $"Temp Delete {Guid.NewGuid():N}",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await TestAuth.ReadJsonAsync<IdDto>(createRes);
        Assert.NotNull(created);

        using var deleteReq = TestAuth.Authed(HttpMethod.Delete, $"/api/projects/{created.Id}", admin.Token);
        var deleteRes = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        using var getReq = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{created.Id}", admin.Token);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);

        using var listReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", admin.Token);
        var listRes = await _client.SendAsync(listReq);
        listRes.EnsureSuccessStatusCode();
        var projects = await TestAuth.ReadJsonAsync<List<IdDto>>(listRes) ?? [];
        Assert.DoesNotContain(projects, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Soft_deleted_task_is_excluded_from_progress_and_hidden()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");

        using var createProject = TestAuth.Authed(HttpMethod.Post, "/api/projects", admin.Token, new
        {
            name = $"SoftDel Progress {Guid.NewGuid():N}",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var projectRes = await _client.SendAsync(createProject);
        Assert.Equal(HttpStatusCode.Created, projectRes.StatusCode);
        var project = await TestAuth.ReadJsonAsync<ProjectDto>(projectRes);
        Assert.NotNull(project);

        async Task<IdDto> CreateTask(string title, string status)
        {
            using var req = TestAuth.Authed(HttpMethod.Post, "/api/tasks", admin.Token, new
            {
                projectId = project.Id,
                stageId = (long?)null,
                title,
                description = (string?)null,
                status,
                priority = "MEDIUM",
                assigneeId = (long?)null,
                startDate = (string?)null,
                dueDate = (string?)null,
                estimatedHours = 1,
                actualHours = (decimal?)null
            });
            var res = await _client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            return (await TestAuth.ReadJsonAsync<IdDto>(res))!;
        }

        var done = await CreateTask("Done task", "DONE");
        var open = await CreateTask("Open task", "TODO");

        using var getBefore = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{project.Id}", admin.Token);
        var beforeRes = await _client.SendAsync(getBefore);
        beforeRes.EnsureSuccessStatusCode();
        var before = await TestAuth.ReadJsonAsync<ProjectDto>(beforeRes);
        Assert.Equal(50m, before!.Progress);

        using var del = TestAuth.Authed(HttpMethod.Delete, $"/api/tasks/{done.Id}", admin.Token);
        var delRes = await _client.SendAsync(del);
        Assert.Equal(HttpStatusCode.NoContent, delRes.StatusCode);

        using var getTask = TestAuth.Authed(HttpMethod.Get, $"/api/tasks/{done.Id}", admin.Token);
        var getTaskRes = await _client.SendAsync(getTask);
        Assert.Equal(HttpStatusCode.NotFound, getTaskRes.StatusCode);

        using var listTasks = TestAuth.Authed(HttpMethod.Get, $"/api/tasks?projectId={project.Id}", admin.Token);
        var listRes = await _client.SendAsync(listTasks);
        listRes.EnsureSuccessStatusCode();
        var tasks = await TestAuth.ReadJsonAsync<List<IdDto>>(listRes) ?? [];
        Assert.DoesNotContain(tasks, t => t.Id == done.Id);
        Assert.Contains(tasks, t => t.Id == open.Id);

        using var getAfter = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{project.Id}", admin.Token);
        var afterRes = await _client.SendAsync(getAfter);
        afterRes.EnsureSuccessStatusCode();
        var after = await TestAuth.ReadJsonAsync<ProjectDto>(afterRes);
        // Only open TODO remains → progress 0 (soft-deleted DONE excluded immediately).
        Assert.Equal(0m, after!.Progress);
    }

    [Fact]
    public async Task Reopening_done_task_clears_completion_and_recalculates_progress()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");

        using var createProject = TestAuth.Authed(HttpMethod.Post, "/api/projects", admin.Token, new
        {
            name = $"Reopen Progress {Guid.NewGuid():N}",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var projectRes = await _client.SendAsync(createProject);
        Assert.Equal(HttpStatusCode.Created, projectRes.StatusCode);
        var project = await TestAuth.ReadJsonAsync<ProjectDto>(projectRes);
        Assert.NotNull(project);

        using var createTask = TestAuth.Authed(HttpMethod.Post, "/api/tasks", admin.Token, new
        {
            projectId = project.Id,
            stageId = (long?)null,
            title = "Complete then reopen",
            description = (string?)null,
            status = "DONE",
            priority = "MEDIUM",
            assigneeId = (long?)null,
            startDate = (string?)null,
            dueDate = (string?)null,
            estimatedHours = 1,
            actualHours = (decimal?)null
        });
        var taskRes = await _client.SendAsync(createTask);
        Assert.Equal(HttpStatusCode.Created, taskRes.StatusCode);
        var task = await TestAuth.ReadJsonAsync<IdDto>(taskRes);
        Assert.NotNull(task);

        using var getBefore = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{project.Id}", admin.Token);
        var before = await TestAuth.ReadJsonAsync<ProjectDto>(await _client.SendAsync(getBefore));
        Assert.Equal(100m, before!.Progress);

        using var reopen = TestAuth.Authed(HttpMethod.Patch, $"/api/tasks/{task.Id}/status", admin.Token, new
        {
            status = "IN_PROGRESS",
            reopenReason = "Needs rework"
        });
        var reopenRes = await _client.SendAsync(reopen);
        Assert.Equal(HttpStatusCode.OK, reopenRes.StatusCode);
        var reopened = await TestAuth.ReadJsonAsync<TaskStatusDto>(reopenRes);
        Assert.Equal("IN_PROGRESS", reopened!.Status);

        using var getAfter = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{project.Id}", admin.Token);
        var after = await TestAuth.ReadJsonAsync<ProjectDto>(await _client.SendAsync(getAfter));
        Assert.Equal(0m, after!.Progress);
    }

    private sealed record IdDto(long Id);
    private sealed record ProjectDto(long Id, decimal Progress);
    private sealed record TaskStatusDto(long Id, string Status);
}

public class PlanAndKpiTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public PlanAndKpiTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Kpi_duplicate_period_is_rejected()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");
        var period = $"P-{Guid.NewGuid():N}";

        using var dirReq = TestAuth.Authed(HttpMethod.Get, "/api/directory/users", admin.Token);
        var dirRes = await _client.SendAsync(dirReq);
        dirRes.EnsureSuccessStatusCode();
        var users = await TestAuth.ReadJsonAsync<List<IdDto>>(dirRes) ?? [];
        var employee = users.First(u => u.Id != admin.User.Id);

        var payload = new
        {
            userId = employee.Id,
            period,
            tasksCompleted = 5,
            tasksOnTime = 4,
            score = 80,
            notes = "first"
        };

        using var firstReq = TestAuth.Authed(HttpMethod.Post, "/api/employee-kpis", admin.Token, payload);
        var firstRes = await _client.SendAsync(firstReq);
        Assert.Equal(HttpStatusCode.Created, firstRes.StatusCode);

        using var dupReq = TestAuth.Authed(HttpMethod.Post, "/api/employee-kpis", admin.Token, payload);
        var dupRes = await _client.SendAsync(dupReq);
        Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);
        var body = await dupRes.Content.ReadAsStringAsync();
        Assert.Contains("already exists", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Approved_evaluation_requires_reopen_before_recalculate()
    {
        var email = $"kpilock-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Lock Org", email);

        using var periodReq = TestAuth.Authed(HttpMethod.Post, "/api/kpi/periods", session.Token, new
        {
            name = $"Lock-{Guid.NewGuid():N}",
            startDate = "2026-01-01",
            endDate = "2026-12-31"
        });
        var periodRes = await _client.SendAsync(periodReq);
        Assert.Equal(HttpStatusCode.Created, periodRes.StatusCode);
        var period = await TestAuth.ReadJsonAsync<IdDto>(periodRes);
        Assert.NotNull(period);

        using var defReq = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = $"T{Guid.NewGuid():N}"[..8].ToUpperInvariant(),
            name = "Tasks completed",
            description = (string?)null,
            weight = 100m,
            targetValue = (decimal?)null,
            formula = "TASKS_COMPLETED",
            higherIsBetter = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(defReq)).StatusCode);

        using var openReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/periods/{period.Id}/open", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(openReq)).StatusCode);

        using var ensureReq = TestAuth.Authed(HttpMethod.Post,
            $"/api/kpi/evaluations/ensure?periodId={period.Id}&userId={session.User.Id}", session.Token);
        var ensureRes = await _client.SendAsync(ensureReq);
        ensureRes.EnsureSuccessStatusCode();
        var eval = await TestAuth.ReadJsonAsync<EvalDto>(ensureRes);
        Assert.NotNull(eval);

        using var calcReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/calculate", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(calcReq)).StatusCode);

        using var submitReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/submit", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(submitReq)).StatusCode);

        using var approveReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/approve", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(approveReq)).StatusCode);

        using var blockedCalc = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/calculate", session.Token);
        var blockedRes = await _client.SendAsync(blockedCalc);
        Assert.True(
            blockedRes.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict,
            $"Expected 400/409, got {(int)blockedRes.StatusCode}");
        var blockedBody = await blockedRes.Content.ReadAsStringAsync();
        Assert.Contains("locked", blockedBody, StringComparison.OrdinalIgnoreCase);

        using var reopenReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/reopen", session.Token,
            new { reason = "Fix scoring error" });
        var reopenRes = await _client.SendAsync(reopenReq);
        Assert.Equal(HttpStatusCode.OK, reopenRes.StatusCode);
        var reopened = await TestAuth.ReadJsonAsync<EvalDto>(reopenRes);
        Assert.Equal("DRAFT", reopened!.Status);
        Assert.Equal("Fix scoring error", reopened.ReopenReason);

        using var calcAfter = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/evaluations/{eval.Id}/calculate", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(calcAfter)).StatusCode);
    }

    [Fact]
    public async Task Period_kpi_snapshot_keeps_historical_weight_after_definition_change()
    {
        var email = $"kpisnap-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Snap Org", email);
        var code = $"W{Guid.NewGuid():N}"[..8].ToUpperInvariant();

        using var periodReq = TestAuth.Authed(HttpMethod.Post, "/api/kpi/periods", session.Token, new
        {
            name = $"Snap-{Guid.NewGuid():N}",
            startDate = "2026-01-01",
            endDate = "2026-12-31"
        });
        var period = await TestAuth.ReadJsonAsync<IdDto>(await _client.SendAsync(periodReq));
        Assert.NotNull(period);

        using var defReq = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code,
            name = "On time",
            description = (string?)null,
            weight = 100m,
            targetValue = 10m,
            formula = "TASKS_ON_TIME",
            higherIsBetter = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(defReq)).StatusCode);

        using var openReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/periods/{period.Id}/open", session.Token);
        var opened = await TestAuth.ReadJsonAsync<PeriodDto>(await _client.SendAsync(openReq));
        Assert.NotNull(opened?.KpiSnapshots);
        var openedSnap = Assert.Single(opened.KpiSnapshots!, s => s.Code == code);
        Assert.Equal(100m, openedSnap.Weight);
        Assert.Equal(10m, openedSnap.TargetValue);
        Assert.Equal("On time", openedSnap.Name);

        // Manager changes live weight/name after period started.
        using var changeDef = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code,
            name = "On time RENAMED",
            description = (string?)null,
            weight = 100m,
            targetValue = 99m,
            formula = "TASKS_ON_TIME",
            higherIsBetter = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(changeDef)).StatusCode);

        using var periodsReq = TestAuth.Authed(HttpMethod.Get, "/api/kpi/periods", session.Token);
        var periods = await TestAuth.ReadJsonAsync<List<PeriodDto>>(await _client.SendAsync(periodsReq)) ?? [];
        var frozen = periods.First(p => p.Id == period.Id).KpiSnapshots!.First(s => s.Code == code);
        Assert.Equal(100m, frozen.Weight);
        Assert.Equal(10m, frozen.TargetValue);
        Assert.Equal("On time", frozen.Name);
    }

    [Fact]
    public async Task Open_period_rejects_when_active_weights_do_not_total_100()
    {
        var email = $"kpiw-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Weight Org", email);

        using var defReq = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "PARTIAL",
            name = "Partial",
            description = (string?)null,
            weight = 40m,
            targetValue = (decimal?)null,
            formula = "TASKS_COMPLETED",
            higherIsBetter = true,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(defReq)).StatusCode);

        using var periodReq = TestAuth.Authed(HttpMethod.Post, "/api/kpi/periods", session.Token, new
        {
            name = $"BadW-{Guid.NewGuid():N}",
            startDate = "2026-01-01",
            endDate = "2026-12-31"
        });
        var period = await TestAuth.ReadJsonAsync<IdDto>(await _client.SendAsync(periodReq));
        Assert.NotNull(period);

        using var openReq = TestAuth.Authed(HttpMethod.Post, $"/api/kpi/periods/{period.Id}/open", session.Token);
        var openRes = await _client.SendAsync(openReq);
        Assert.True(openRes.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        Assert.Contains("100%", await openRes.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Upsert_rejects_negative_or_over_100_weight()
    {
        var email = $"kpiu-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Upsert Org", email);

        using var neg = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "BAD_NEG",
            name = "Bad",
            description = (string?)null,
            weight = -5m,
            targetValue = (decimal?)null,
            formula = "TASKS_COMPLETED",
            higherIsBetter = true,
            isActive = true
        });
        var negRes = await _client.SendAsync(neg);
        Assert.True(negRes.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);

        using var over = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "BAD_OVER",
            name = "Bad",
            description = (string?)null,
            weight = 101m,
            targetValue = (decimal?)null,
            formula = "TASKS_COMPLETED",
            higherIsBetter = true,
            isActive = true
        });
        var overRes = await _client.SendAsync(over);
        Assert.True(overRes.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Upsert_scopes_kpi_code_uniqueness_per_role()
    {
        var email = $"kpidup-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Dup Org", email);

        using var first = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "ON_TIME",
            name = "On time",
            description = (string?)null,
            weight = 50m,
            targetValue = (decimal?)null,
            formula = "TASKS_ON_TIME",
            higherIsBetter = true,
            isActive = true,
            appliesToRole = "EMPLOYEE"
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(first)).StatusCode);

        // Same code + same role is an upsert (idempotent update), not a second row.
        using var same = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "ON_TIME",
            name = "On time updated",
            description = (string?)null,
            weight = 60m,
            targetValue = (decimal?)null,
            formula = "TASKS_ON_TIME",
            higherIsBetter = true,
            isActive = true,
            appliesToRole = "EMPLOYEE"
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(same)).StatusCode);

        // Same code for a different role is allowed.
        using var otherRole = TestAuth.Authed(HttpMethod.Put, "/api/kpi/definitions", session.Token, new
        {
            code = "ON_TIME",
            name = "On time PM",
            description = (string?)null,
            weight = 100m,
            targetValue = (decimal?)null,
            formula = "TASKS_ON_TIME",
            higherIsBetter = true,
            isActive = true,
            appliesToRole = "PROJECT_MANAGER"
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(otherRole)).StatusCode);

        using var listReq = TestAuth.Authed(HttpMethod.Get, "/api/kpi/definitions", session.Token);
        var defs = await TestAuth.ReadJsonAsync<List<DefDto>>(await _client.SendAsync(listReq)) ?? [];
        Assert.Equal(2, defs.Count(d => d.Code == "ON_TIME"));
        Assert.Contains(defs, d => d.Code == "ON_TIME" && d.AppliesToRole == "EMPLOYEE" && d.Weight == 60m);
        Assert.Contains(defs, d => d.Code == "ON_TIME" && d.AppliesToRole == "PROJECT_MANAGER");
    }

    [Fact]
    public async Task Plan_project_limit_is_enforced()
    {
        var email = $"plan-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Limited Org", email);

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var sub = await db.SubscriptionSet.FirstAsync(s => s.OrganizationId == user.OrganizationId);
            sub.PlanCode = "STARTER";
        });

        for (var i = 0; i < 3; i++)
        {
            using var okReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject($"P{i}"));
            var okRes = await _client.SendAsync(okReq);
            Assert.Equal(HttpStatusCode.Created, okRes.StatusCode);
        }

        using var overReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject("Over"));
        var overRes = await _client.SendAsync(overReq);
        Assert.Equal(HttpStatusCode.PaymentRequired, overRes.StatusCode);
        var body = await overRes.Content.ReadAsStringAsync();
        Assert.Contains("allows up to", body, StringComparison.OrdinalIgnoreCase);
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

    private sealed record IdDto(long Id);
    private sealed record EvalDto(long Id, string Status, string? ReopenReason);
    private sealed record PeriodDto(long Id, List<SnapDto>? KpiSnapshots);
    private sealed record SnapDto(string Code, string Name, decimal Weight, decimal? TargetValue);
    private sealed record DefDto(string Code, decimal Weight, string? AppliesToRole);
}

public class AuthTokenTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public AuthTokenTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_token_reuse_revokes_entire_family()
    {
        var email = $"refresh-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Refresh Org", email);
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = await TestAuth.ReadJsonAsync<RefreshDto>(first);
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrWhiteSpace(rotated.RefreshToken));
        Assert.NotEqual(session.RefreshToken, rotated.RefreshToken);

        // Attacker (or stale client) replays the pre-rotation token → family kill.
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Legitimate post-rotation token is also dead after reuse detection.
        var afterTheft = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var familyTokens = await db.RefreshTokenSet
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            Assert.NotEmpty(familyTokens);
            Assert.Contains(familyTokens, t => t.ReuseDetectedAt != null);
            Assert.All(familyTokens, t => Assert.NotNull(t.RevokedAt));
        });
    }

    private sealed record RefreshDto(string Token, string? RefreshToken);
}
