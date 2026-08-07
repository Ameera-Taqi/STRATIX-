using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stratix.Api.Auth;
using Stratix.Api.Filters;
using Stratix.Application;
using Stratix.Infrastructure;
using Stratix.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://0.0.0.0:8080");

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions { Indented = false };
});
if (builder.Environment.IsDevelopment())
    builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "HH:mm:ss ");

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<FluentValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Stratix API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Stratix:Jwt:Secret"];
var jwtIssuer = builder.Configuration["Stratix:Jwt:Issuer"] ?? "stratix";
var jwtAudience = builder.Configuration["Stratix:Jwt:Audience"] ?? "stratix-api";
var validateIssuerAudience = builder.Environment.IsProduction()
    || string.Equals(builder.Configuration["Stratix:Jwt:ValidateIssuerAudience"], "true", StringComparison.OrdinalIgnoreCase);

// Fail fast on a missing/insecure JWT secret in Production; warn loudly elsewhere.
var secretIsWeak = string.IsNullOrWhiteSpace(jwtSecret)
    || jwtSecret.Contains("change-this", StringComparison.OrdinalIgnoreCase)
    || jwtSecret.Contains("dev-only", StringComparison.OrdinalIgnoreCase)
    || Encoding.UTF8.GetByteCount(jwtSecret) < 32;
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "Stratix:Jwt:Secret is required. Set it via environment (STRATIX_JWT_SECRET / Stratix__Jwt__Secret), user-secrets, or a gitignored Development settings file.");
if (secretIsWeak)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException(
            "Stratix:Jwt:Secret is missing or insecure. Provide a strong secret (>= 32 bytes) via configuration/environment before running in Production.");
    Console.WriteLine("[stratix][WARN] Weak/default JWT secret in use — set a strong Stratix:Jwt:Secret before production.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = validateIssuerAudience,
            ValidateAudience = validateIssuerAudience,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.SuperAdmin, p => p.RequireRole("SUPER_ADMIN"));
    options.AddPolicy(AuthPolicies.OrgAdmins, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN"));
    options.AddPolicy(AuthPolicies.ProjectManagers, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER"));
    options.AddPolicy(AuthPolicies.TeamLeaders, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER", "TEAM_LEADER"));
    options.AddPolicy(AuthPolicies.TaskContributors, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER", "TEAM_LEADER", "EMPLOYEE"));
    options.AddPolicy(AuthPolicies.LeadersAndExecutives, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER", "TEAM_LEADER", "EXECUTIVE_VIEWER"));
    options.AddPolicy(AuthPolicies.AiAnalysts, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER", "TEAM_LEADER", "EXECUTIVE_VIEWER"));
    options.AddPolicy(AuthPolicies.AllTenantUsers, p =>
        p.RequireRole("SUPER_ADMIN", "ORG_ADMIN", "ADMIN", "PROJECT_MANAGER", "TEAM_LEADER", "EMPLOYEE", "EXECUTIVE_VIEWER"));
});

// Rate limiting — throttles abuse. The "auth" policy caps sign-in/refresh attempts per IP
// to blunt brute-force, on top of per-account lockout.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 20,
                QueueLimit = 0,
            }));
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 300,
                QueueLimit = 0,
            }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration["Stratix:Cors:Origins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(Stratix.Api.PagingHeaders.ExposedNames));
});

var app = builder.Build();

app.UseMiddleware<Stratix.Api.Middleware.RequestTelemetryMiddleware>();
app.UseMiddleware<Stratix.Api.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Stratix:Swagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<Stratix.Api.Middleware.OrganizationAccessMiddleware>();
app.UseAuthorization();
app.MapControllers();

// Tenant files live under Stratix:Storage:Root and are served only through authenticated
// controllers (reports/logo). Do NOT call UseStaticFiles on that directory.

if (builder.Configuration.GetValue("Stratix:SeedData", true))
{
    await DataSeeder.SeedAsync(app.Services, builder.Configuration);
}

app.Run();

public partial class Program;
