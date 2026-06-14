using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Infrastructure.Data;
using StoneBridge.Infrastructure.Data.Repositories;
using StoneBridge.Infrastructure.Services;

namespace StoneBridge.Infrastructure.Extensions;

/// <summary>
/// Registers all Infrastructure layer services into the DI container.
/// Called from Program.cs: builder.Services.AddInfrastructureServices(configuration).
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // ── Dapper global configuration ────────────────────────────────────
        // Maps PostgreSQL snake_case column names to C# PascalCase properties automatically.
        // Without this: Dapper cannot map "gross_length_mm" to "GrossLengthMm".
        // This must be set before any Dapper query executes.
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        // ── PostgreSQL connection pooling ──────────────────────────────────
        // NpgsqlDataSource manages a pool of NpgsqlConnections.
        // Min Pool Size = 5: keeps 5 warm connections alive at all times.
        // Max Pool Size = 100: cap to prevent overwhelming the PostgreSQL server.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured. " +
                "Format: Host=localhost;Port=5432;Database=stonebridge;Username=...;Password=...;");

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource        = dataSourceBuilder.Build();

        services.AddSingleton(dataSource);

        // ── Database factory ───────────────────────────────────────────────
        // Scoped: DbConnectionFactory injects ICurrentTenant (also Scoped/per-request).
        // A Singleton cannot consume a Scoped service — DI validator rejects it at startup.
        // NpgsqlDataSource (the actual connection pool) remains Singleton above.
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        // ── Repositories ───────────────────────────────────────────────────
        // Scoped: one repository instance per HTTP request.
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<ISupplierSlabRepository, SupplierSlabRepository>();
        services.AddScoped<ISupplierProfileRepository, SupplierProfileRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IBundleRepository, BundleRepository>();

        // ── Current tenant service ─────────────────────────────────────────
        // Scoped: reads from HttpContext which is per-request.
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenant, CurrentTenantService>();

        // ── Cache (Redis or in-memory fallback) ────────────────────────────
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName  = "stonebridge:";
            });
        }
        else
        {
            // Development fallback: in-memory cache (not distributed, single-process only)
            services.AddDistributedMemoryCache();
        }

        services.AddScoped<ICacheService, CacheService>();

        return services;
    }
}