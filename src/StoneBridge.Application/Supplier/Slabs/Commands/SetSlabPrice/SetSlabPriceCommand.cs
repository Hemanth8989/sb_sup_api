using MediatR;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.SetSlabPrice;

public sealed record SetSlabPriceCommand(Guid SlabId, decimal? PriceOverride) : IRequest<SupplierSlabDto>;
