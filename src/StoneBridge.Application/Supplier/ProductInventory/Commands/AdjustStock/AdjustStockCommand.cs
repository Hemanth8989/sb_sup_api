using MediatR;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.AdjustStock;

public sealed record AdjustStockCommand(
    Guid     VariantId,
    int      Delta,
    decimal? NewPrice,
    string?  Reason
) : IRequest<ProductVariantInventoryDto>;
