using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class PriceListRepository : IPriceListRepository
{
    private readonly IDbConnectionFactory _db;

    public PriceListRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IReadOnlyList<PriceListDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                pl.id, pl.name, pl.tier, pl.currency,
                pl.valid_from, pl.valid_to, pl.is_active,
                pl.created_at, pl.updated_at,
                COUNT(pli.id) AS item_count
            FROM price_lists pl
            LEFT JOIN price_list_items pli ON pli.price_list_id = pl.id
            WHERE pl.tenant_id = @TenantId
            GROUP BY pl.id
            ORDER BY pl.created_at DESC
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<dynamic>(sql, new { TenantId = tenantId });
        return rows.Select(MapPriceList).ToList();
    }

    public async Task<PriceListDetailDto?> GetByIdAsync(
        Guid tenantId, Guid priceListId, CancellationToken ct = default)
    {
        const string headerSql = """
            SELECT
                pl.id, pl.name, pl.tier, pl.currency,
                pl.valid_from, pl.valid_to, pl.is_active,
                pl.created_at, pl.updated_at,
                COUNT(pli.id) AS item_count
            FROM price_lists pl
            LEFT JOIN price_list_items pli ON pli.price_list_id = pl.id
            WHERE pl.id = @PriceListId AND pl.tenant_id = @TenantId
            GROUP BY pl.id
            """;

        const string itemsSql = """
            SELECT
                pli.id, pli.variant_id,
                pv.variant_name,
                pv.sku,
                pli.unit_price,
                pli.currency
            FROM price_list_items pli
            JOIN product_variants pv ON pv.id = pli.variant_id
            WHERE pli.price_list_id = @PriceListId
            ORDER BY pv.variant_name
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var header = await conn.QueryFirstOrDefaultAsync<dynamic>(headerSql, new { PriceListId = priceListId, TenantId = tenantId });
        if (header is null)
        {
            return null;
        }

        var items = await conn.QueryAsync<PriceListItemDto>(itemsSql, new { PriceListId = priceListId });
        return new PriceListDetailDto
        {
            Id        = header.id,
            Name      = header.name,
            Tier      = header.tier,
            Currency  = header.currency,
            ValidFrom = header.valid_from is null ? null : DateOnly.FromDateTime(header.valid_from),
            ValidTo   = header.valid_to   is null ? null : DateOnly.FromDateTime(header.valid_to),
            IsActive  = header.is_active,
            ItemCount = (int)(header.item_count ?? 0),
            CreatedAt = header.created_at,
            UpdatedAt = header.updated_at,
            Items     = items.ToList()
        };
    }

    public async Task<PriceListDto> CreateAsync(
        Guid tenantId, CreatePriceListRequest req, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO price_lists (tenant_id, name, tier, currency, valid_from, valid_to)
            VALUES (@TenantId, @Name, @Tier, @Currency, @ValidFrom, @ValidTo)
            RETURNING id, name, tier, currency, valid_from, valid_to, is_active, created_at, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstAsync<dynamic>(sql, new
        {
            TenantId  = tenantId,
            req.Name,
            req.Tier,
            Currency  = req.Currency ?? "USD",
            ValidFrom = req.ValidFrom?.ToDateTime(TimeOnly.MinValue),
            ValidTo   = req.ValidTo?.ToDateTime(TimeOnly.MinValue)
        });

        return MapPriceList(row);
    }

    public async Task<PriceListDto> UpdateAsync(
        Guid tenantId, Guid priceListId, UpdatePriceListRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE price_lists
            SET name       = @Name,
                tier       = @Tier,
                currency   = @Currency,
                valid_from = @ValidFrom,
                valid_to   = @ValidTo,
                is_active  = @IsActive,
                updated_at = NOW()
            WHERE id = @PriceListId AND tenant_id = @TenantId
            RETURNING id, name, tier, currency, valid_from, valid_to, is_active, created_at, updated_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
        {
            PriceListId = priceListId,
            TenantId    = tenantId,
            req.Name,
            req.Tier,
            Currency    = req.Currency ?? "USD",
            ValidFrom   = req.ValidFrom?.ToDateTime(TimeOnly.MinValue),
            ValidTo     = req.ValidTo?.ToDateTime(TimeOnly.MinValue),
            req.IsActive
        });

        return row is null ? throw new InvalidOperationException("PriceList not found") : MapPriceList(row);
    }

    public async Task DeleteAsync(Guid tenantId, Guid priceListId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM price_lists WHERE id = @PriceListId AND tenant_id = @TenantId";
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { PriceListId = priceListId, TenantId = tenantId });
    }

    public async Task<PriceListItemDto> UpsertItemAsync(
        Guid tenantId, Guid priceListId, UpsertPriceListItemRequest req, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO price_list_items (price_list_id, variant_id, unit_price)
            VALUES (@PriceListId, @VariantId, @UnitPrice)
            ON CONFLICT (price_list_id, variant_id) DO UPDATE
                SET unit_price = EXCLUDED.unit_price
            RETURNING id, variant_id, unit_price, currency
            """;

        const string variantSql = "SELECT variant_name, sku FROM product_variants WHERE id = @VariantId";

        using var conn = await _db.CreateConnectionAsync(ct);
        var item = await conn.QueryFirstAsync<dynamic>(sql, new
        {
            PriceListId = priceListId,
            req.VariantId,
            req.UnitPrice
        });

        var variant = await conn.QueryFirstOrDefaultAsync<dynamic>(variantSql, new { req.VariantId });

        return new PriceListItemDto
        {
            Id          = item.id,
            VariantId   = item.variant_id,
            VariantName = variant?.variant_name ?? string.Empty,
            Sku         = variant?.sku ?? string.Empty,
            UnitPrice   = item.unit_price,
            Currency    = item.currency
        };
    }

    public async Task RemoveItemAsync(Guid tenantId, Guid priceListId, Guid itemId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM price_list_items WHERE id = @ItemId AND price_list_id = @PriceListId";
        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { ItemId = itemId, PriceListId = priceListId });
    }

    public async Task<PriceListDto> CloneAsync(
        Guid tenantId, Guid priceListId, string newName, CancellationToken ct = default)
    {
        const string cloneSql = """
            INSERT INTO price_lists (tenant_id, name, tier, currency, valid_from, valid_to)
            SELECT tenant_id, @NewName, tier, currency, NULL, NULL
            FROM price_lists
            WHERE id = @PriceListId AND tenant_id = @TenantId
            RETURNING id, name, tier, currency, valid_from, valid_to, is_active, created_at, updated_at
            """;

        const string itemsSql = """
            INSERT INTO price_list_items (price_list_id, variant_id, unit_price)
            SELECT @NewId, variant_id, unit_price
            FROM price_list_items
            WHERE price_list_id = @SourceId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(cloneSql, new
        {
            PriceListId = priceListId,
            TenantId    = tenantId,
            NewName     = newName
        });

        if (row is null)
        {
            throw new InvalidOperationException("Source price list not found");
        }

        Guid newId = row.id;
        await conn.ExecuteAsync(itemsSql, new { NewId = newId, SourceId = priceListId });

        return MapPriceList(row);
    }

    private static PriceListDto MapPriceList(dynamic r) => new()
    {
        Id        = r.id,
        Name      = r.name,
        Tier      = r.tier,
        Currency  = r.currency,
        ValidFrom = r.valid_from is null ? null : DateOnly.FromDateTime(r.valid_from),
        ValidTo   = r.valid_to   is null ? null : DateOnly.FromDateTime(r.valid_to),
        IsActive  = r.is_active,
        ItemCount = (int)(r.item_count ?? 0),
        CreatedAt = r.created_at,
        UpdatedAt = r.updated_at
    };
}
