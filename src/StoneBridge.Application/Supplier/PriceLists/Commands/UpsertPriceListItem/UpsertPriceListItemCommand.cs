using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.UpsertPriceListItem;

public sealed record UpsertPriceListItemCommand(
    Guid    PriceListId,
    Guid    VariantId,
    decimal UnitPrice
) : IRequest<PriceListItemDto>;
