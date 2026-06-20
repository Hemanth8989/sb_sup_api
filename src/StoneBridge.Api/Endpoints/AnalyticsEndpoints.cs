using Dapper;
using Microsoft.AspNetCore.Mvc;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Api.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/supplier/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        // ── GET /analytics/summary ─────────────────────────────────────────
        group.MapGet("/summary", async (
            IDbConnectionFactory db,
            ICurrentTenant       currentTenant,
            CancellationToken    ct) =>
        {
            const string sql = """
                SELECT
                    COUNT(*) FILTER (WHERE status = 'available')                   AS available,
                    COUNT(*) FILTER (WHERE status = 'reserved')                    AS reserved,
                    COUNT(*) FILTER (WHERE status = 'hold')                        AS hold,
                    COUNT(*) FILTER (WHERE status = 'allocated')                   AS allocated,
                    COUNT(*) FILTER (WHERE status = 'shipped')                     AS shipped,
                    COUNT(*)                                                        AS total_slabs,
                    COALESCE(SUM(net_sqft) FILTER (WHERE status = 'available'), 0) AS total_sqft_on_hand
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
        })
        .WithName("GetInventorySummary")
        .WithSummary("Slab status counts + total sqft on hand.");

        // ── GET /analytics/inventory ───────────────────────────────────────
        group.MapGet("/inventory", async (
            [FromQuery] string range,         // 30d | 90d | 365d
            IDbConnectionFactory db,
            ICurrentTenant       currentTenant,
            CancellationToken    ct) =>
        {
            var days   = ParseDays(range);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            using var conn = await db.CreateConnectionAsync(ct);

            // Status breakdown
            const string statusSql = """
                SELECT
                    status,
                    COUNT(*)       AS slab_count,
                    COALESCE(SUM(net_sqft), 0) AS total_sqft
                FROM slabs
                WHERE tenant_id = @TenantId
                  AND is_active  = TRUE
                GROUP BY status
                ORDER BY slab_count DESC
                """;

            // Sqft trend: daily available sqft added vs slabs leaving available
            const string trendSql = """
                SELECT
                    DATE_TRUNC('week', created_at) AS week,
                    COUNT(*)                        AS slabs_added,
                    COALESCE(SUM(net_sqft), 0)      AS sqft_added
                FROM slabs
                WHERE tenant_id  = @TenantId
                  AND created_at >= @Cutoff
                GROUP BY 1
                ORDER BY 1
                """;

            // Slow-movers: available > 90 days
            const string slowMoverSql = """
                SELECT
                    s.id,
                    s.internal_ref,
                    s.material_name,
                    s.net_sqft,
                    s.price_override,
                    v.base_price,
                    COALESCE(s.price_override, v.base_price) * s.net_sqft AS est_value,
                    EXTRACT(DAY FROM NOW() - s.status_changed)::int        AS days_in_status,
                    s.rack_location,
                    w.name AS warehouse_name
                FROM slabs s
                JOIN product_variants v ON v.id = s.variant_id
                LEFT JOIN warehouses  w ON w.id = s.warehouse_id
                WHERE s.tenant_id = @TenantId
                  AND s.status    = 'available'
                  AND s.is_active = TRUE
                  AND s.status_changed < NOW() - INTERVAL '90 days'
                ORDER BY days_in_status DESC
                LIMIT 10
                """;

            // Avg days in status per material
            const string avgDaysSql = """
                SELECT
                    material_name,
                    status,
                    ROUND(AVG(EXTRACT(DAY FROM NOW() - status_changed))::numeric, 1) AS avg_days
                FROM slabs
                WHERE tenant_id = @TenantId
                  AND is_active  = TRUE
                GROUP BY material_name, status
                ORDER BY material_name, status
                """;

            var p = new { TenantId = currentTenant.TenantId, Cutoff = cutoff };

            var statusRows    = await conn.QueryAsync(statusSql,    new { TenantId = currentTenant.TenantId });
            var trendRows     = await conn.QueryAsync(trendSql,     p);
            var slowMovers    = await conn.QueryAsync(slowMoverSql, new { TenantId = currentTenant.TenantId });
            var avgDaysRows   = await conn.QueryAsync(avgDaysSql,   new { TenantId = currentTenant.TenantId });

            return Results.Ok(new
            {
                statusBreakdown = statusRows.Select(r => new
                {
                    status     = (string)r.status,
                    slabCount  = (int)r.slab_count,
                    totalSqft  = (decimal)r.total_sqft,
                }),
                weeklyTrend = trendRows.Select(r => new
                {
                    week       = ((DateTime)r.week).ToString("yyyy-MM-dd"),
                    slabsAdded = (int)r.slabs_added,
                    sqftAdded  = (decimal)r.sqft_added,
                }),
                slowMovers = slowMovers.Select(r => new
                {
                    id            = (string)r.id.ToString(),
                    internalRef   = (string)r.internal_ref,
                    materialName  = (string)r.material_name,
                    netSqft       = (decimal)r.net_sqft,
                    estValue      = r.est_value == null ? (decimal?)null : (decimal)r.est_value,
                    daysInStatus  = (int)r.days_in_status,
                    rackLocation  = (string?)r.rack_location,
                    warehouseName = (string?)r.warehouse_name,
                }),
                avgDaysInStatus = avgDaysRows.Select(r => new
                {
                    materialName = (string)r.material_name,
                    status       = (string)r.status,
                    avgDays      = (decimal)r.avg_days,
                }),
            });
        })
        .WithName("GetInventoryAnalytics")
        .WithSummary("Inventory health — status breakdown, weekly trend, slow-movers.");

        // ── GET /analytics/revenue ─────────────────────────────────────────
        group.MapGet("/revenue", async (
            [FromQuery] string range,
            IDbConnectionFactory db,
            ICurrentTenant       currentTenant,
            CancellationToken    ct) =>
        {
            var days   = ParseDays(range);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            using var conn = await db.CreateConnectionAsync(ct);

            // Monthly revenue from shipped/received POs (line items)
            const string monthlyRevSql = """
                SELECT
                    DATE_TRUNC('month', po.shipped_at) AS month,
                    COUNT(DISTINCT po.id)               AS po_count,
                    COALESCE(SUM(li.line_total), 0)     AS revenue
                FROM purchase_orders po
                JOIN po_line_items li ON li.po_id = po.id
                WHERE po.supplier_id = @TenantId
                  AND po.status      IN ('shipped', 'received', 'closed')
                  AND po.shipped_at  >= @Cutoff
                GROUP BY 1
                ORDER BY 1
                """;

            // Revenue by material
            const string materialRevSql = """
                SELECT
                    s.material_name,
                    COUNT(DISTINCT s.id)             AS slabs_sold,
                    COALESCE(SUM(s.net_sqft), 0)     AS sqft_sold,
                    COALESCE(SUM(li.line_total), 0)  AS revenue
                FROM slabs s
                JOIN po_line_items li ON li.slab_id = s.id
                JOIN purchase_orders po ON po.id = li.po_id
                WHERE s.tenant_id  = @TenantId
                  AND po.status    IN ('shipped', 'received', 'closed')
                  AND po.shipped_at >= @Cutoff
                GROUP BY s.material_name
                ORDER BY revenue DESC
                LIMIT 10
                """;

            // Price override usage
            const string overrideSql = """
                SELECT
                    COUNT(*) FILTER (WHERE price_override IS NOT NULL AND status = 'sold') AS with_override,
                    COUNT(*) FILTER (WHERE price_override IS NULL     AND status = 'sold') AS without_override
                FROM slabs
                WHERE tenant_id = @TenantId
                """;

            var p = new { TenantId = currentTenant.TenantId, Cutoff = cutoff };

            var monthlyRev  = await conn.QueryAsync(monthlyRevSql,  p);
            var materialRev = await conn.QueryAsync(materialRevSql, p);
            var overrideRow = await conn.QuerySingleAsync(overrideSql, new { TenantId = currentTenant.TenantId });

            return Results.Ok(new
            {
                monthlyRevenue = monthlyRev.Select(r => new
                {
                    month   = ((DateTime)r.month).ToString("yyyy-MM"),
                    poCount = (int)r.po_count,
                    revenue = (decimal)r.revenue,
                }),
                revenueByMaterial = materialRev.Select(r => new
                {
                    materialName = (string)r.material_name,
                    slabsSold    = (int)r.slabs_sold,
                    sqftSold     = (decimal)r.sqft_sold,
                    revenue      = (decimal)r.revenue,
                }),
                priceOverrideUsage = new
                {
                    withOverride    = (int)overrideRow.with_override,
                    withoutOverride = (int)overrideRow.without_override,
                },
            });
        })
        .WithName("GetRevenueAnalytics")
        .WithSummary("Revenue by period and material.");

        // ── GET /analytics/orders ──────────────────────────────────────────
        group.MapGet("/orders", async (
            [FromQuery] string range,
            IDbConnectionFactory db,
            ICurrentTenant       currentTenant,
            CancellationToken    ct) =>
        {
            var days   = ParseDays(range);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            using var conn = await db.CreateConnectionAsync(ct);

            const string funnelSql = """
                SELECT
                    status,
                    COUNT(*) AS po_count
                FROM purchase_orders
                WHERE supplier_id  = @TenantId
                  AND created_at  >= @Cutoff
                GROUP BY status
                """;

            // Avg hours between key stage transitions
            const string stageSql = """
                SELECT
                    ROUND(AVG(EXTRACT(EPOCH FROM (acked_at - sent_at))  / 3600)::numeric, 1) AS avg_hrs_to_ack,
                    ROUND(AVG(EXTRACT(EPOCH FROM (shipped_at - acked_at)) / 3600)::numeric, 1) AS avg_hrs_to_ship,
                    ROUND(AVG(EXTRACT(EPOCH FROM (received_at - shipped_at)) / 3600)::numeric, 1) AS avg_hrs_to_receive
                FROM purchase_orders
                WHERE supplier_id = @TenantId
                  AND created_at >= @Cutoff
                  AND acked_at IS NOT NULL
                """;

            const string topBuyersSql = """
                SELECT
                    t.name          AS fabricator_name,
                    COUNT(po.id)    AS po_count,
                    SUM(po.total_amount) AS total_spend
                FROM purchase_orders po
                JOIN tenants t ON t.id = po.fabricator_id
                WHERE po.supplier_id  = @TenantId
                  AND po.created_at  >= @Cutoff
                GROUP BY t.name
                ORDER BY total_spend DESC
                LIMIT 10
                """;

            const string cancelRateSql = """
                SELECT
                    COUNT(*) FILTER (WHERE status IN ('cancelled','disputed'))  AS bad_count,
                    COUNT(*)                                                     AS total_count
                FROM purchase_orders
                WHERE supplier_id = @TenantId
                  AND created_at >= @Cutoff
                """;

            var p = new { TenantId = currentTenant.TenantId, Cutoff = cutoff };

            var funnelRows  = await conn.QueryAsync(funnelSql,     p);
            var stageRow    = await conn.QuerySingleAsync(stageSql, p);
            var topBuyers   = await conn.QueryAsync(topBuyersSql,  p);
            var cancelRow   = await conn.QuerySingleAsync(cancelRateSql, p);

            var total      = (int)cancelRow.total_count;
            var cancelRate = total == 0 ? 0m : Math.Round((decimal)cancelRow.bad_count / total * 100, 1);

            return Results.Ok(new
            {
                funnel = funnelRows.Select(r => new
                {
                    status  = (string)r.status,
                    poCount = (int)r.po_count,
                }),
                stageTiming = new
                {
                    avgHrsToAck     = stageRow.avg_hrs_to_ack     == null ? (decimal?)null : (decimal)stageRow.avg_hrs_to_ack,
                    avgHrsToShip    = stageRow.avg_hrs_to_ship    == null ? (decimal?)null : (decimal)stageRow.avg_hrs_to_ship,
                    avgHrsToReceive = stageRow.avg_hrs_to_receive == null ? (decimal?)null : (decimal)stageRow.avg_hrs_to_receive,
                },
                topBuyers = topBuyers.Select(r => new
                {
                    fabricatorName = (string)r.fabricator_name,
                    poCount        = (int)r.po_count,
                    totalSpend     = (decimal)r.total_spend,
                }),
                cancellationRate = cancelRate,
            });
        })
        .WithName("GetOrderAnalytics")
        .WithSummary("PO funnel, stage timings, top buyers.");

        // ── GET /analytics/connections ─────────────────────────────────────
        group.MapGet("/connections", async (
            [FromQuery] string range,
            IDbConnectionFactory db,
            ICurrentTenant       currentTenant,
            CancellationToken    ct) =>
        {
            var days   = ParseDays(range);
            var cutoff = DateTime.UtcNow.AddDays(-days);

            using var conn = await db.CreateConnectionAsync(ct);

            const string statusTierSql = """
                SELECT
                    status,
                    pricing_tier,
                    COUNT(*) AS cnt
                FROM connections
                WHERE supplier_id = @TenantId
                GROUP BY status, pricing_tier
                """;

            const string growthSql = """
                SELECT
                    DATE_TRUNC('month', connected_at) AS month,
                    COUNT(*)                           AS new_connections
                FROM connections
                WHERE supplier_id   = @TenantId
                  AND connected_at >= @Cutoff
                GROUP BY 1
                ORDER BY 1
                """;

            const string geographySql = """
                SELECT
                    fp.city,
                    fp.state_province AS state,
                    COUNT(*)          AS connection_count
                FROM connections c
                JOIN fabricator_profiles fp ON fp.tenant_id = c.fabricator_id
                WHERE c.supplier_id = @TenantId
                  AND c.status      = 'active'
                  AND fp.city IS NOT NULL
                GROUP BY fp.city, fp.state_province
                ORDER BY connection_count DESC
                LIMIT 20
                """;

            var p = new { TenantId = currentTenant.TenantId, Cutoff = cutoff };

            var statusTier = await conn.QueryAsync(statusTierSql, new { TenantId = currentTenant.TenantId });
            var growth     = await conn.QueryAsync(growthSql,     p);
            var geography  = await conn.QueryAsync(geographySql,  new { TenantId = currentTenant.TenantId });

            return Results.Ok(new
            {
                byStatusAndTier = statusTier.Select(r => new
                {
                    status      = (string)r.status,
                    pricingTier = (string)r.pricing_tier,
                    count       = (int)r.cnt,
                }),
                monthlyGrowth = growth.Select(r => new
                {
                    month          = ((DateTime)r.month).ToString("yyyy-MM"),
                    newConnections = (int)r.new_connections,
                }),
                topCities = geography.Select(r => new
                {
                    city            = (string?)r.city,
                    state           = (string?)r.state,
                    connectionCount = (int)r.connection_count,
                }),
            });
        })
        .WithName("GetConnectionAnalytics")
        .WithSummary("Connection tier breakdown, growth trend, geography.");
    }

    private static int ParseDays(string? range) => range switch
    {
        "7d"   => 7,
        "90d"  => 90,
        "365d" => 365,
        _      => 30,
    };
}
