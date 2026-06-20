using MediatR;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.ProductInventory.DTOs;

namespace StoneBridge.Application.Supplier.ProductInventory.Queries.GetProducts;

public sealed record GetProductsQuery(ProductInventoryFilterParams Filter)
    : IRequest<PagedResult<ProductInventoryDto>>;
