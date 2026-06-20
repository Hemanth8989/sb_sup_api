using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceLists;

public sealed record GetPriceListsQuery : IRequest<IReadOnlyList<PriceListDto>>;
