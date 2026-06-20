using MediatR;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Commands.UpdateSlab;

public sealed record UpdateSlabCommand(Guid SlabId, UpdateSlabRequest Request) : IRequest<SupplierSlabDto>;
