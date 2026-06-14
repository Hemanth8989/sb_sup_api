using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Commands.TransferSlabs;

public sealed record TransferSlabsCommand(
    Guid                 SourceWarehouseId,
    TransferSlabsRequest Request
) : IRequest<int>;
