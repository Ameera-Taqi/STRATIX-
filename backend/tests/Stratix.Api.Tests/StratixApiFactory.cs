using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Api.Tests;

public sealed class StratixApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"StratixTests-{Guid.NewGuid():N}";
    private readonly string _storageRoot;

    static StratixApiFactory()
    {
        // Environment vars override appsettings so JWT signing/validation stay aligned in tests.
        Environment.SetEnvironmentVariable("Stratix__Jwt__Secret", "integration-test-secret-key-at-least-32-chars!!");
        Environment.SetEnvironmentVariable("Stratix__Jwt__Issuer", "stratix");
        Environment.SetEnvironmentVariable("Stratix__Jwt__Audience", "stratix-api");
        Environment.SetEnvironmentVariable("Stratix__PasswordReset__TokenPepper", "integration-test-pepper-value");
        Environment.SetEnvironmentVariable("Stratix__SeedData", "true");
        Environment.SetEnvironmentVariable("Stratix__SyncSeedPasswords", "true");
        Environment.SetEnvironmentVariable("Stratix__AllowLoginAliases", "true");
    }

    public StratixApiFactory()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), "stratix-test-storage", _dbName);
        Directory.CreateDirectory(_storageRoot);
        Environment.SetEnvironmentVariable("Stratix__Storage__Root", _storageRoot);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=unused;Database=unused;",
                ["Stratix:Jwt:Secret"] = "integration-test-secret-key-at-least-32-chars!!",
                ["Stratix:Jwt:Issuer"] = "stratix",
                ["Stratix:Jwt:Audience"] = "stratix-api",
                ["Stratix:PasswordReset:TokenPepper"] = "integration-test-pepper-value",
                ["Stratix:SeedData"] = "true",
                ["Stratix:SyncSeedPasswords"] = "true",
                ["Stratix:AllowLoginAliases"] = "true",
                ["Stratix:Storage:Root"] = _storageRoot,
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<StratixDbContext>));
            services.RemoveAll(typeof(StratixDbContext));
            services.RemoveAll(typeof(IHostedService));

            services.AddDbContext<StratixDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }
}
