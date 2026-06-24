namespace Stratix.Application.DTOs;

public record HealthResponse(string Application, string Status, string Database, string? DatabaseProduct, string? DatabaseError);
