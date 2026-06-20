using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.UpdatePriceList;

public sealed record UpdatePriceListCommand(
    Guid    PriceListId,
    string  Name,
    string? Description,
    string  Tier,
    string  Currency,
    bool    IsActive
) : IRequest<PriceListDto>;
