using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Queries.GetPriceList;

public sealed record GetPriceListQuery(Guid PriceListId) : IRequest<PriceListDetailDto?>;
