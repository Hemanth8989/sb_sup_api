using MediatR;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.DeletePriceList;

public sealed record DeletePriceListCommand(Guid PriceListId) : IRequest;
