using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stratix.Application;
using Stratix.Infrastructure;
using Stratix.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls(builder.Configuration["ASPNETCORE_URLS"] ?? "http://0.0.0.0:8080");

builder.Services.AddControllers()
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

var jwtSecret = builder.Configuration["Stratix:Jwt:Secret"] ?? "change-this-secret-in-production-min-256-bits-long";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(builder.Configuration["Stratix:Cors:Origins"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? ["http://localhost:4200"])
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<Stratix.Api.Middleware.ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue("Stratix:Swagger", false))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (builder.Configuration.GetValue("Stratix:SeedData", true))
{
    await DataSeeder.SeedAsync(app.Services, builder.Configuration);
}

app.Run();
