using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.CreatePriceList;

public sealed record CreatePriceListCommand(
    string  Name,
    string? Description,
    string  Tier,
    string  Currency
) : IRequest<PriceListDto>;
