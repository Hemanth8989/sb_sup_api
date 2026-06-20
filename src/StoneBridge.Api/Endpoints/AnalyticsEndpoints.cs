using Dapper;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        group.MapGet("/summary", async (
            IDbConnectionFactory  db,
            ICurrentTenant        currentTenant,
            CancellationToken     ct) =>
        {
            const string sql = """
                SELECT
                    COUNT(*) FILTER (WHERE status = 'available')                       AS available,
                    COUNT(*) FILTER (WHERE status = 'reserved')                        AS reserved,
                    COUNT(*) FILTER (WHERE status = 'hold')                            AS hold,
                    COUNT(*) FILTER (WHERE status = 'allocated')                       AS allocated,
                    COUNT(*) FILTER (WHERE status = 'shipped')                         AS shipped,
                    COUNT(*)                                                            AS total_slabs,
                    COALESCE(SUM(net_sqft) FILTER (WHERE status = 'available'), 0)     AS total_sqft_on_hand
                FROM slabs
                WHERE tenant_id = @TenantId
                  AND is_active = TRUE
                """;

            using var conn = await db.CreateConnectionAsync(ct);
            var row = await conn.QuerySingleAsync(sql, new { TenantId = currentTenant.TenantId });

            return Results.Ok(new
            {
                available       = (int)row.available,
                reserved        = (int)row.reserved,
                hold            = (int)row.hold,
                allocated       = (int)row.allocated,
                shipped         = (int)row.shipped,
                totalSlabs      = (int)row.total_slabs,
                totalSqftOnHand = (decimal)row.total_sqft_on_hand,
            });
        });
    }
}
