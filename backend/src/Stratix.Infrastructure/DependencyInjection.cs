using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stratix.Application.Interfaces;
using Stratix.Infrastructure.Auth;
using Stratix.Infrastructure.Mail;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_CONNECTION"]
            ?? "Server=localhost,1433;Database=StratixDB;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;Encrypt=False";

        services.AddDbContext<StratixDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<StratixDbContext>());
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IPasswordResetMailService, SmtpPasswordResetMailService>();
        services.AddHttpContextAccessor();
        services.AddHostedService<Background.ReportOrphanCleanupService>();

        return services;
    }
}
