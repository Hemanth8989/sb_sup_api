using MediatR;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.UpsertProduct;

public sealed record UpsertProductCommand(
    Guid?   ProductId,
    string  CategoryCode,
    string  Name,
    string? Brand,
    string? Description,
    Guid?   VariantId,
    string  Sku,
    string  VariantName,
    string  UnitOfMeasure,
    decimal BasePrice,
    int     QtyAvailable,
    int?    LeadTimeDays
) : IRequest<ProductInventoryDto>;
