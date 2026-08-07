namespace Stratix.Application.DTOs;

public record HealthResponse(string Application, string Status, string Database, string? DatabaseProduct, string? DatabaseError);

public record LivenessResponse(string Status, string Application);

public record ReadinessCheck(string Name, string Status, string? Detail = null);

public record ReadinessResponse(
    string Status,
    string Application,
    IReadOnlyList<ReadinessCheck> Checks);
