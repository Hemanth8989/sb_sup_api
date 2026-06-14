using System.Data;

namespace StoneBridge.Application.Common.Interfaces;

/// <summary>
/// Creates open database connections for use with Dapper.
/// Each repository method calls CreateConnectionAsync(), uses it, and disposes it.
/// The underlying NpgsqlDataSource handles connection pooling transparently.
/// CRITICAL: Every connection returned by this factory has already had the PostgreSQL
/// session variables app.tenant_id and app.tenant_type set for Row Level Security.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Create and open an async PostgreSQL connection with RLS context already set.
    /// Caller is responsible for disposing the returned connection.
    /// </summary>
    Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
}