using MediatR;
using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.GetStockMovements;

public sealed record GetStockMovementsQuery(Guid WarehouseId, int Limit = 100)
    : IRequest<IReadOnlyList<StockMovementDto>>;
