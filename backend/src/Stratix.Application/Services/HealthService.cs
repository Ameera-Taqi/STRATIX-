using Stratix.Application.DTOs;
using Stratix.Application.Interfaces;

namespace Stratix.Application.Services;

public class HealthService : IHealthService
{
    private readonly IApplicationDbContext _db;

    public HealthService(IApplicationDbContext db) => _db = db;

    public async Task<HealthResponse> GetHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var ok = await _db.CanConnectAsync(ct);
            return new HealthResponse("stratix-api", ok ? "UP" : "DOWN", ok ? "UP" : "DOWN", _db.GetDatabaseProductName(), null);
        }
        catch (Exception ex)
        {
            return new HealthResponse("stratix-api", "UP", "DOWN", null, ex.Message);
        }
    }
}
