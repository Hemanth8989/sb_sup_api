using Dapper;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.PurchaseOrders.DTOs;

namespace StoneBridge.Infrastructure.Data.Repositories;

public sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private readonly IDbConnectionFactory _db;

    public PurchaseOrderRepository(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<PurchaseOrderDto>> GetAllAsync(
        Guid supplierId, PoFilterParams filter, CancellationToken ct = default)
    {
        var offset = (filter.Page - 1) * filter.PerPage;
        const string sql = """
            SELECT
                po.id, po.po_number, po.fabricator_id, po.supplier_id, po.job_id,
                po.status, po.subtotal, po.discount_amount, po.tax_amount,
                po.shipping_amount, po.total_amount, po.currency,
                po.requested_delivery, po.confirmed_delivery,
                po.tracking_number, po.carrier,
                po.fabricator_notes, po.supplier_notes, po.internal_ref,
                po.sent_at, po.acked_at, po.shipped_at, po.received_at,
                po.created_at, po.updated_at,
                t.name AS fabricator_name,
                COUNT(*) OVER() AS total_count
            FROM purchase_orders po
            JOIN tenants t ON t.id = po.fabricator_id
            WHERE po.supplier_id = @SupplierId
              AND (@Status IS NULL OR po.status = @Status)
            ORDER BY po.created_at DESC
            LIMIT @PerPage OFFSET @Offset
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var rows = await conn.QueryAsync<dynamic>(sql, new
        {
            SupplierId = supplierId,
            filter.Status,
            filter.PerPage,
            Offset = offset
        });

        var list = new List<PurchaseOrderDto>();
        int total = 0;
        foreach (var r in rows)
        {
            total = (int)(r.total_count ?? 0);
            list.Add(MapPo(r));
        }

        return PagedResult<PurchaseOrderDto>.Create(list, total, filter.Page, filter.PerPage);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid supplierId, Guid poId, CancellationToken ct = default)
    {
        const string poSql = """
            SELECT
                po.id, po.po_number, po.fabricator_id, po.supplier_id, po.job_id,
                po.status, po.subtotal, po.discount_amount, po.tax_amount,
                po.shipping_amount, po.total_amount, po.currency,
                po.requested_delivery, po.confirmed_delivery,
                po.tracking_number, po.carrier,
                po.fabricator_notes, po.supplier_notes, po.internal_ref,
                po.sent_at, po.acked_at, po.shipped_at, po.received_at,
                po.created_at, po.updated_at,
                t.name AS fabricator_name
            FROM purchase_orders po
            JOIN tenants t ON t.id = po.fabricator_id
            WHERE po.id = @PoId AND po.supplier_id = @SupplierId
            """;

        const string linesSql = """
            SELECT
                li.id, li.variant_id, li.slab_id,
                pv.name AS variant_name, pv.sku,
                s.internal_ref AS slab_ref,
                li.quantity, li.unit_of_measure, li.unit_price, li.line_total,
                li.status, li.decline_reason, li.counter_price, li.counter_note,
                li.updated_at
            FROM po_line_items li
            JOIN product_variants pv ON pv.id = li.variant_id
            LEFT JOIN slabs s ON s.id = li.slab_id
            WHERE li.po_id = @PoId
            ORDER BY li.created_at
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        var poRow = await conn.QueryFirstOrDefaultAsync<dynamic>(poSql, new { PoId = poId, SupplierId = supplierId });
        if (poRow is null)
        {
            return null;
        }

        var lines = await conn.QueryAsync<PoLineItemDto>(linesSql, new { PoId = poId });
        return MapPo(poRow, lines.ToList());
    }

    public async Task<PurchaseOrderDto> AcknowledgeAsync(
        Guid supplierId, Guid poId, AcknowledgePoRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE purchase_orders
            SET status           = 'acknowledged',
                acked_at         = NOW(),
                supplier_notes   = COALESCE(@SupplierNotes, supplier_notes),
                confirmed_delivery = CASE WHEN @ConfirmedDelivery IS NOT NULL
                                    THEN @ConfirmedDelivery::DATE
                                    ELSE confirmed_delivery END,
                updated_at       = NOW()
            WHERE id = @PoId AND supplier_id = @SupplierId AND status = 'sent'
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            PoId          = poId,
            SupplierId    = supplierId,
            req.SupplierNotes,
            ConfirmedDelivery = req.ConfirmedDelivery
        });

        return (await GetByIdAsync(supplierId, poId, ct))!;
    }

    public async Task<PurchaseOrderDto> ShipAsync(
        Guid supplierId, Guid poId, ShipPoRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE purchase_orders
            SET status            = 'shipped',
                shipped_at        = NOW(),
                tracking_number   = @TrackingNumber,
                carrier           = @Carrier,
                confirmed_delivery = CASE WHEN @ConfirmedDelivery IS NOT NULL
                                    THEN @ConfirmedDelivery::DATE
                                    ELSE confirmed_delivery END,
                updated_at        = NOW()
            WHERE id = @PoId AND supplier_id = @SupplierId
              AND status IN ('acknowledged','confirmed')
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            PoId              = poId,
            SupplierId        = supplierId,
            req.TrackingNumber,
            req.Carrier,
            ConfirmedDelivery = req.ConfirmedDelivery
        });

        return (await GetByIdAsync(supplierId, poId, ct))!;
    }

    public async Task<PurchaseOrderDto> UpdateStatusAsync(
        Guid supplierId, Guid poId, UpdatePoStatusRequest req, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE purchase_orders
            SET status         = @Status,
                status_changed = NOW(),
                supplier_notes = COALESCE(@SupplierNotes, supplier_notes),
                updated_at     = NOW()
            WHERE id = @PoId AND supplier_id = @SupplierId
            """;

        using var conn = await _db.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            PoId          = poId,
            SupplierId    = supplierId,
            req.Status,
            req.SupplierNotes
        });

        return (await GetByIdAsync(supplierId, poId, ct))!;
    }

    private static PurchaseOrderDto MapPo(dynamic r, IReadOnlyList<PoLineItemDto>? lines = null)
        => new()
        {
            Id                = r.id,
            PoNumber          = r.po_number,
            FabricatorId      = r.fabricator_id,
            FabricatorName    = r.fabricator_name ?? string.Empty,
            SupplierId        = r.supplier_id,
            JobId             = r.job_id,
            Status            = r.status,
            Subtotal          = r.subtotal,
            DiscountAmount    = r.discount_amount,
            TaxAmount         = r.tax_amount,
            ShippingAmount    = r.shipping_amount,
            TotalAmount       = r.total_amount,
            Currency          = r.currency,
            RequestedDelivery = r.requested_delivery is null ? null : DateOnly.FromDateTime(r.requested_delivery),
            ConfirmedDelivery = r.confirmed_delivery is null ? null : DateOnly.FromDateTime(r.confirmed_delivery),
            TrackingNumber    = r.tracking_number,
            Carrier           = r.carrier,
            FabricatorNotes   = r.fabricator_notes,
            SupplierNotes     = r.supplier_notes,
            InternalRef       = r.internal_ref,
            SentAt            = r.sent_at,
            AckedAt           = r.acked_at,
            ShippedAt         = r.shipped_at,
            ReceivedAt        = r.received_at,
            CreatedAt         = r.created_at,
            UpdatedAt         = r.updated_at,
            LineItems         = lines ?? []
        };
}
