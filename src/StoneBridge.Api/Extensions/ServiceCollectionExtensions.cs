using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StoneBridge.Application;
using StoneBridge.Application.Common.Behaviours;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Infrastructure.Extensions;

namespace StoneBridge.Api.Extensions;

/// <summary>
/// Extension methods that register services into the DI container.
/// Keeps Program.cs clean — one line per layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register Application layer services: MediatR pipeline, FluentValidation validators.
    /// </summary>
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // MediatR — scans Application assembly for all IRequestHandler<,> implementations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly);

            // Pipeline behaviours run in this order:
            // 1. Validation  — reject invalid requests before the handler
            // 2. Logging     — log entry, exit, and errors
            // 3. Performance — measure elapsed time, warn if slow
            // 4. Transaction — wrap ITransactionalCommand in BEGIN/COMMIT/ROLLBACK
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehaviour<,>));
        });

        // FluentValidation — scans Application assembly for all AbstractValidator<T> classes
        services.AddValidatorsFromAssembly(
            typeof(ApplicationAssemblyMarker).Assembly,
            includeInternalTypes: true);

        return services;
    }

    /// <summary>
    /// Register Infrastructure layer services (delegates to InfrastructureServiceExtensions).
    /// Separate method so the call site reads: AddInfrastructureServices(config).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration          configuration)
        => StoneBridge.Infrastructure.Extensions.InfrastructureServiceExtensions
            .AddInfrastructureServices(services, configuration);

    /// <summary>
    /// Register API layer services: JSON options, authentication, CORS, OpenAPI.
    /// </summary>
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── JSON serialisation ─────────────────────────────────────────────
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        // ── Swagger / OpenAPI ──────────────────────────────────────────────
        // AddEndpointsApiExplorer is required for Minimal APIs — MVC uses AddControllers instead.
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title       = "StoneBridge API",
                Version     = "v1",
                Description = "Supplier–Fabricator real-time stone inventory and purchasing platform.",
            });

            // JWT Bearer auth — adds the Authorize button in Swagger UI
            c.AddSecurityDefinition("Bearer", new()
            {
                Name         = "Authorization",
                Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description  = "Enter your Clerk JWT token. The 'Bearer ' prefix is added automatically.",
            });

            c.AddSecurityRequirement(new()
            {
                {
                    new()
                    {
                        Reference = new()
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id   = "Bearer",
                        },
                    },
                    []
                },
            });
        });

        // ── CORS ───────────────────────────────────────────────────────────
        var allowedOrigins = configuration
            .GetSection("AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:5174"];

        services.AddCors(options =>
        {
            options.AddPolicy("StoneBridgeCors", policy =>
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
        });

        // ── Authentication (Clerk JWT) ─────────────────────────────────────
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Clerk:Authority"]
                    ?? throw new InvalidOperationException(
                        "Clerk:Authority is not configured. " +
                        "Set it to your Clerk instance URL: https://{instance}.clerk.accounts.dev");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidAudience            = configuration["Clerk:Audience"],
                    ClockSkew                = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();

        return services;
    }
}

