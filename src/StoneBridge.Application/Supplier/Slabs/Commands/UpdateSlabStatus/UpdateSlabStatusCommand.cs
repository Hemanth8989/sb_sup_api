using MediatR;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlabStatus;

public sealed record UpdateSlabStatusCommand(Guid SlabId, string Status) : IRequest<SupplierSlabDto>;
