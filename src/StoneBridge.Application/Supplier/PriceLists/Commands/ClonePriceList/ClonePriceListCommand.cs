using MediatR;
using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.ClonePriceList;

public sealed record ClonePriceListCommand(Guid SourceId, string NewName) : IRequest<PriceListDto>;
