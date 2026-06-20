using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class ProductInventoryRepository : IProductInventoryRepository
{
    private readonly IDbConnectionFactory _db;

    public ProductInventoryRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ProductInventoryDto>> GetAllAsync(
        Guid tenantId, ProductInventoryFilterParams filter, CancellationToken ct = default)
    {
        var offset = (filter.Page - 1) * filter.PerPage;

        const string sql = """
            SELECT
                p.id, p.category_code, pc.label AS category_label,
                p.name, p.brand, p.short_description AS description,
                p.is_active, p.created_at, p.updated_at,
                COUNT(*) OVER() AS total_count
            FROM products p
            JOIN product_categories pc ON pc.code = p.category_code
            WHERE p.tenant_id  = @TenantId
              AND p.is_active   = TRUE
              AND p.category_code <> 'slab'
              AND (@CategoryCode IS NULL OR p.category_code = @CategoryCode)
              AND (@Search IS NULL OR (
                    p.name ILIKE @SearchPattern OR
                    COALESCE(p.brand,'') ILIKE @SearchPattern
              ))
            ORDER BY pc.sort_order, p.name
            LIMIT @PerPage OFFSET @Offset
            """;

        const string variantsSql = """
            SELECT
                pv.id, pv.product_id, pv.sku, pv.variant_name,
                pv.unit_of_measure, pv.base_price, pv.currency,
                pv.qty_available, pv.qty_reserved,
                pv.status, pv.lead_time_days, pv.primary_photo_url,
                pv.updated_at
            FROM product_variants pv
            WHERE pv.tenant_id       = @TenantId
              AND pv.is_slab_variant = FALSE
              AND pv.product_id      = ANY(@ProductIds)
            ORDER BY pv.sku
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        var rows = (await conn.QueryAsync<dynamic>(sql, new
        {
            TenantId     = tenantId,
            filter.CategoryCode,
            Search       = filter.Search,
            SearchPattern = filter.Search is null ? null : $"%{filter.Search}%",
            filter.PerPage,
            Offset       = offset
        })).ToList();

        if (rows.Count == 0)
        {
            return PagedResult<ProductInventoryDto>.Empty(filter.Page, filter.PerPage);
        }

        int total      = (int)(rows[0].total_count ?? 0);
        var productIds = rows.Select(r => (Guid)r.id).ToArray();

        var variants = (await conn.QueryAsync<ProductVariantInventoryDto>(variantsSql, new
        {
            TenantId   = tenantId,
            ProductIds = productIds
        })).ToList();

        var variantsByProduct = variants.GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ProductVariantInventoryDto>)g.ToList());

        var products = rows.Select(r =>
        {
            var pid = (Guid)r.id;
            return new ProductInventoryDto
            {
                Id            = pid,
                CategoryCode  = r.category_code,
                CategoryLabel = r.category_label,
                Name          = r.name,
                Brand         = r.brand,
                Description   = r.description,
                IsActive      = r.is_active,
                CreatedAt     = r.created_at,
                UpdatedAt     = r.updated_at,
                Variants      = variantsByProduct.TryGetValue(pid, out var v) ? v : []
            };
        }).ToList();

        return PagedResult<ProductInventoryDto>.Create(products, total, filter.Page, filter.PerPage);
    }

    public async Task<ProductInventoryDto?> GetByIdAsync(Guid tenantId, Guid productId, CancellationToken ct = default)
    {
        const string productSql = """
            SELECT
                p.id, p.category_code, pc.label AS category_label,
                p.name, p.brand, p.short_description AS description,
                p.is_active, p.created_at, p.updated_at
            FROM products p
            JOIN product_categories pc ON pc.code = p.category_code
            WHERE p.id = @ProductId AND p.tenant_id = @TenantId
            """;

        const string variantsSql = """
            SELECT
                pv.id, pv.product_id, pv.sku, pv.variant_name,
                pv.unit_of_measure, pv.base_price, pv.currency,
                pv.qty_available, pv.qty_reserved,
                pv.status, pv.lead_time_days, pv.primary_photo_url,
                pv.updated_at
            FROM product_variants pv
            WHERE pv.product_id = @ProductId AND pv.tenant_id = @TenantId
              AND pv.is_slab_variant = FALSE
            ORDER BY pv.sku
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(productSql,
            new { ProductId = productId, TenantId = tenantId });

        if (row is null)
        {
            return null;
        }

        var variants = await conn.QueryAsync<ProductVariantInventoryDto>(variantsSql,
            new { ProductId = productId, TenantId = tenantId });

        return new ProductInventoryDto
        {
            Id            = row.id,
            CategoryCode  = row.category_code,
            CategoryLabel = row.category_label,
            Name          = row.name,
            Brand         = row.brand,
            Description   = row.description,
            IsActive      = row.is_active,
            CreatedAt     = row.created_at,
            UpdatedAt     = row.updated_at,
            Variants      = variants.ToList()
        };
    }

    public async Task<ProductInventoryDto> UpsertAsync(
        Guid tenantId, UpsertProductRequest req, CancellationToken ct = default)
    {
        const string productSql = """
            INSERT INTO products (id, tenant_id, category_code, name, brand, short_description)
            VALUES (@Id, @TenantId, @CategoryCode, @Name, @Brand, @Description)
            ON CONFLICT (id) DO UPDATE
                SET category_code    = EXCLUDED.category_code,
                    name             = EXCLUDED.name,
                    brand            = EXCLUDED.brand,
                    short_description = EXCLUDED.short_description,
                    updated_at       = NOW()
            RETURNING id
            """;

        const string variantSql = """
            INSERT INTO product_variants (
                id, product_id, tenant_id, sku, variant_name,
                unit_of_measure, base_price, qty_available,
                lead_time_days, is_slab_variant, status
            ) VALUES (
                @Id, @ProductId, @TenantId, @Sku, @VariantName,
                @UnitOfMeasure, @BasePrice, @QtyAvailable,
                @LeadTimeDays, FALSE, 'active'
            )
            ON CONFLICT (tenant_id, sku) DO UPDATE
                SET variant_name    = EXCLUDED.variant_name,
                    unit_of_measure = EXCLUDED.unit_of_measure,
                    base_price      = EXCLUDED.base_price,
                    qty_available   = EXCLUDED.qty_available,
                    lead_time_days  = EXCLUDED.lead_time_days,
                    updated_at      = NOW()
            RETURNING id
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        var productId = req.ProductId ?? Guid.NewGuid();
        await conn.ExecuteAsync(productSql, new
        {
            Id           = productId,
            TenantId     = tenantId,
            req.CategoryCode,
            req.Name,
            req.Brand,
            Description  = req.Description
        });

        var variantId = req.VariantId ?? Guid.NewGuid();
        await conn.ExecuteAsync(variantSql, new
        {
            Id             = variantId,
            ProductId      = productId,
            TenantId       = tenantId,
            req.Sku,
            req.VariantName,
            req.UnitOfMeasure,
            req.BasePrice,
            req.QtyAvailable,
            req.LeadTimeDays
        });

        return (await GetByIdAsync(tenantId, productId, ct))!;
    }

    public async Task<ProductVariantInventoryDto> AdjustStockAsync(
        Guid tenantId, Guid variantId, int delta, decimal? newPrice, CancellationToken ct = default)
    {
        // Fetch current state first so we can record price history if needed
        const string fetchSql = """
            SELECT id, product_id, sku, variant_name, unit_of_measure,
                   base_price, currency, qty_available, qty_reserved,
                   status, lead_time_days, primary_photo_url, updated_at
            FROM product_variants
            WHERE id = @VariantId AND tenant_id = @TenantId AND is_slab_variant = FALSE
            """;

        const string updateSql = """
            UPDATE product_variants
            SET qty_available = qty_available + @Delta,
                base_price    = COALESCE(@NewPrice, base_price),
                status        = CASE
                                    WHEN qty_available + @Delta = 0 THEN 'out_of_stock'
                                    WHEN status = 'out_of_stock' AND qty_available + @Delta > 0 THEN 'active'
                                    ELSE status
                                END,
                updated_at    = NOW()
            WHERE id = @VariantId AND tenant_id = @TenantId
              AND (qty_available + @Delta) >= 0
            RETURNING id, product_id, sku, variant_name, unit_of_measure,
                      base_price, currency, qty_available, qty_reserved,
                      status, lead_time_days, primary_photo_url, updated_at
            """;

        const string priceSql = """
            INSERT INTO product_price_history
                (variant_id, tenant_id, old_price, new_price, currency)
            VALUES (@VariantId, @TenantId, @OldPrice, @NewPrice, @Currency)
            """;

        using var conn = await _db.CreateConnectionAsync(ct);

        var current = await conn.QueryFirstOrDefaultAsync<ProductVariantInventoryDto>(fetchSql,
            new { VariantId = variantId, TenantId = tenantId });

        if (current is null)
        {
            throw new InvalidOperationException($"Variant {variantId} not found.");
        }

        var updated = await conn.QueryFirstOrDefaultAsync<ProductVariantInventoryDto>(updateSql,
            new { VariantId = variantId, TenantId = tenantId, Delta = delta, NewPrice = newPrice });

        if (updated is null)
        {
            throw new InvalidOperationException(
                $"Cannot adjust stock: result would fall below zero. Current qty: {current.QtyAvailable}, delta: {delta}.");
        }

        if (newPrice.HasValue && newPrice.Value != current.BasePrice)
        {
            await conn.ExecuteAsync(priceSql, new
            {
                VariantId = variantId,
                TenantId  = tenantId,
                OldPrice  = current.BasePrice,
                NewPrice  = newPrice.Value,
                Currency  = current.Currency
            });
        }

        return updated;
    }

    public async Task DeactivateAsync(Guid tenantId, Guid productId, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE products
            SET is_active = FALSE, updated_at = NOW()
            WHERE id = @ProductId AND tenant_id = @TenantId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new { ProductId = productId, TenantId = tenantId });
    }
}
