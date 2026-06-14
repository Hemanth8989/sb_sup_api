using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

/// <summary>
/// Dapper repository for supplier_profiles table.
/// All writes use INSERT ... ON CONFLICT DO UPDATE (upsert) so new tenants
/// don't need a separate "create profile" step — the first PUT creates the row.
/// </summary>
public sealed class SupplierProfileRepository : ISupplierProfileRepository
{
    private readonly IDbConnectionFactory _db;

    public SupplierProfileRepository(IDbConnectionFactory db) => _db = db;

    // ── Default notification preferences applied to new supplier rows ──────
    private static readonly JsonObject DefaultNotificationPrefs = JsonNode.Parse("""
        {
          "new_po":               { "inApp": true,  "email": true,  "sms": false },
          "po_unacked_24h":       { "inApp": true,  "email": true,  "sms": false },
          "connection_requested": { "inApp": true,  "email": true,  "sms": false },
          "connection_approved":  { "inApp": true,  "email": false, "sms": false },
          "price_changed":        { "inApp": false, "email": true,  "sms": false },
          "low_stock_warning":    { "inApp": true,  "email": true,  "sms": false }
        }
        """)!.AsObject();

    // ── GET profile ────────────────────────────────────────────────────────
    public async Task<SupplierProfileDto?> GetProfileAsync(Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                tenant_id, display_name, logo_url, description, website, phone,
                address_line1, address_line2, city, state_province, postal_code, country,
                established_year, verified, verified_at,
                notification_prefs,
                updated_at
            FROM supplier_profiles
            WHERE tenant_id = @tenantId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<SupplierProfileRow>(sql, new { tenantId });
        return row is null ? null : MapToDto(row);
    }

    // ── UPSERT profile ─────────────────────────────────────────────────────
    public async Task<SupplierProfileDto> UpsertProfileAsync(
        Guid tenantId, UpdateProfileRequest req, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO supplier_profiles (
                tenant_id, display_name, description, website, phone,
                address_line1, address_line2, city, state_province, postal_code, country,
                established_year, notification_prefs
            ) VALUES (
                @tenantId, @displayName, @description, @website, @phone,
                @addressLine1, @addressLine2, @city, @stateProvince, @postalCode,
                UPPER(COALESCE(@country, 'US')),
                @establishedYear,
                @defaultPrefs::jsonb
            )
            ON CONFLICT (tenant_id) DO UPDATE SET
                display_name     = EXCLUDED.display_name,
                description      = EXCLUDED.description,
                website          = EXCLUDED.website,
                phone            = EXCLUDED.phone,
                address_line1    = EXCLUDED.address_line1,
                address_line2    = EXCLUDED.address_line2,
                city             = EXCLUDED.city,
                state_province   = EXCLUDED.state_province,
                postal_code      = EXCLUDED.postal_code,
                country          = EXCLUDED.country,
                established_year = EXCLUDED.established_year,
                updated_at       = NOW()
            RETURNING
                tenant_id, display_name, logo_url, description, website, phone,
                address_line1, address_line2, city, state_province, postal_code, country,
                established_year, verified, verified_at,
                notification_prefs, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleAsync<SupplierProfileRow>(sql, new
        {
            tenantId,
            displayName    = req.DisplayName,
            description    = req.Description,
            website        = req.Website,
            phone          = req.Phone,
            addressLine1   = req.AddressLine1,
            addressLine2   = req.AddressLine2,
            city           = req.City,
            stateProvince  = req.StateProvince,
            postalCode     = req.PostalCode,
            country        = req.Country ?? "US",
            establishedYear = req.EstablishedYear,
            defaultPrefs   = DefaultNotificationPrefs.ToJsonString(),
        });

        return MapToDto(row);
    }

    // ── GET stats ──────────────────────────────────────────────────────────
    public async Task<SupplierStatsDto?> GetStatsAsync(Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                avg_lead_days, fulfillment_rate, avg_response_hrs,
                total_slabs_sold, warehouse_count, updated_at
            FROM supplier_profiles
            WHERE tenant_id = @tenantId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QuerySingleOrDefaultAsync<SupplierStatsRow>(sql, new { tenantId });
        if (row is null)
        {
            return null;
        }

        return new SupplierStatsDto
        {
            AvgLeadDays     = row.AvgLeadDays,
            FulfillmentRate = row.FulfillmentRate,
            AvgResponseHrs  = row.AvgResponseHrs,
            TotalSlabsSold  = row.TotalSlabsSold,
            WarehouseCount  = row.WarehouseCount,
            UpdatedAt       = row.UpdatedAt,
        };
    }

    // ── GET notification prefs ─────────────────────────────────────────────
    public async Task<JsonObject> GetNotificationPrefsAsync(Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "SELECT notification_prefs FROM supplier_profiles WHERE tenant_id = @tenantId";

        using var conn = await _db.CreateConnectionAsync(ct);
        var json = await conn.QuerySingleOrDefaultAsync<string>(sql, new { tenantId });

        if (string.IsNullOrWhiteSpace(json))
        {
            return DefaultNotificationPrefs;
        }

        return JsonNode.Parse(json)?.AsObject() ?? DefaultNotificationPrefs;
    }

    // ── UPDATE notification prefs ──────────────────────────────────────────
    public async Task<JsonObject> UpdateNotificationPrefsAsync(
        Guid tenantId, JsonObject prefs, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE supplier_profiles
            SET    notification_prefs = @prefs::jsonb,
                   updated_at         = NOW()
            WHERE  tenant_id = @tenantId
            RETURNING notification_prefs
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var json = await conn.QuerySingleOrDefaultAsync<string>(sql, new
        {
            tenantId,
            prefs = prefs.ToJsonString(),
        });

        return string.IsNullOrWhiteSpace(json)
            ? prefs
            : JsonNode.Parse(json)?.AsObject() ?? prefs;
    }

    // ── Mapping helpers ────────────────────────────────────────────────────
    private static SupplierProfileDto MapToDto(SupplierProfileRow row)
    {
        JsonObject notifPrefs;
        try
        {
            notifPrefs = string.IsNullOrWhiteSpace(row.NotificationPrefs)
                ? DefaultNotificationPrefs
                : JsonNode.Parse(row.NotificationPrefs)?.AsObject() ?? DefaultNotificationPrefs;
        }
        catch (JsonException)
        {
            notifPrefs = DefaultNotificationPrefs;
        }

        return new SupplierProfileDto
        {
            TenantId          = row.TenantId,
            DisplayName       = row.DisplayName,
            LogoUrl           = row.LogoUrl,
            Description       = row.Description,
            Website           = row.Website,
            Phone             = row.Phone,
            AddressLine1      = row.AddressLine1,
            AddressLine2      = row.AddressLine2,
            City              = row.City,
            StateProvince     = row.StateProvince,
            PostalCode        = row.PostalCode,
            Country           = row.Country,
            EstablishedYear   = row.EstablishedYear,
            Verified          = row.Verified,
            VerifiedAt        = row.VerifiedAt,
            NotificationPrefs = notifPrefs,
            UpdatedAt         = row.UpdatedAt,
        };
    }

    // ── Dapper row models ──────────────────────────────────────────────────
    private sealed class SupplierProfileRow
    {
        public Guid      TenantId          { get; init; }
        public string    DisplayName       { get; init; } = string.Empty;
        public string?   LogoUrl           { get; init; }
        public string?   Description       { get; init; }
        public string?   Website           { get; init; }
        public string?   Phone             { get; init; }
        public string?   AddressLine1      { get; init; }
        public string?   AddressLine2      { get; init; }
        public string?   City              { get; init; }
        public string?   StateProvince     { get; init; }
        public string?   PostalCode        { get; init; }
        public string    Country           { get; init; } = "US";
        public int?      EstablishedYear   { get; init; }
        public bool      Verified          { get; init; }
        public DateTime? VerifiedAt        { get; init; }
        public string?   NotificationPrefs { get; init; }
        public DateTime  UpdatedAt         { get; init; }
    }

    private sealed class SupplierStatsRow
    {
        public decimal? AvgLeadDays     { get; init; }
        public decimal? FulfillmentRate { get; init; }
        public decimal? AvgResponseHrs  { get; init; }
        public int      TotalSlabsSold  { get; init; }
        public int      WarehouseCount  { get; init; }
        public DateTime? UpdatedAt      { get; init; }
    }
}
