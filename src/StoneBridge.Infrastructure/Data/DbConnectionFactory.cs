using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Infrastructure.Data;

/// <summary>
/// Creates PostgreSQL connections using Npgsql.
/// CRITICAL: Every connection this factory returns has already set the PostgreSQL session
/// variables app.tenant_id and app.tenant_type so that RLS policies filter rows correctly.
///
/// Why SET instead of SET LOCAL?
/// Dapper uses implicit transactions per query (AutoCommit = on).
/// SET LOCAL is transaction-scoped and would be lost at each statement boundary.
/// SET is session-scoped — it persists for the entire connection lifetime.
/// Since we use a new connection per repository call (then dispose it), SET is safe.
///
/// Background workers (SyncEventWorker etc.) have no JWT context.
/// They catch UnauthorizedAccessException and set empty strings, so RLS
/// returns no rows for those connections (safe — workers use direct admin queries).
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource                   _dataSource;
    private readonly ICurrentTenant                     _currentTenant;
    private readonly ILogger<DbConnectionFactory>       _logger;

    public DbConnectionFactory(
        NpgsqlDataSource              dataSource,
        ICurrentTenant                currentTenant,
        ILogger<DbConnectionFactory>  logger)
    {
        _dataSource    = dataSource;
        _currentTenant = currentTenant;
        _logger        = logger;
    }

    /// <inheritdoc />
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await SetRlsContextAsync(connection, cancellationToken);
        return connection;
    }

    private async Task SetRlsContextAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        string tenantId;
        string tenantType;

        try
        {
            tenantId   = _currentTenant.TenantId.ToString();
            tenantType = _currentTenant.TenantType;
        }
        catch (UnauthorizedAccessException)
        {
            // No JWT context — background service or unauthenticated request
            // Empty strings cause RLS current_setting('app.tenant_id', TRUE) to return NULL
            // which matches no rows in any policy, keeping the connection safe
            _logger.LogDebug(
                "No tenant context available when creating connection. " +
                "Setting empty RLS context (background service mode).");

            tenantId   = string.Empty;
            tenantType = string.Empty;
        }

        // PostgreSQL SET does not support parameterised values ($1 syntax is rejected).
        // Values come from validated JWT claims; sanitise by stripping single quotes.
        var safeTenantId   = tenantId.Replace("'", "");
        var safeTenantType = tenantType.Replace("'", "");

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SET app.tenant_id   = '{safeTenantId}';
            SET app.tenant_type = '{safeTenantType}';
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}