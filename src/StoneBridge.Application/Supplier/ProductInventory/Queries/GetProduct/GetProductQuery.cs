using MediatR;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Queries.GetProduct;

public sealed record GetProductQuery(Guid ProductId) : IRequest<ProductInventoryDto?>;
