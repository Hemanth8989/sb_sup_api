using MediatR;

namespace StoneBridge.Application.Supplier.PriceLists.Commands.RemovePriceListItem;

public sealed record RemovePriceListItemCommand(Guid PriceListId, Guid ItemId) : IRequest;
