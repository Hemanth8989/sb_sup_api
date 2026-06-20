using MediatR;

namespace StoneBridge.Application.Supplier.ProductInventory.Commands.DeactivateProduct;

public sealed record DeactivateProductCommand(Guid ProductId) : IRequest;
