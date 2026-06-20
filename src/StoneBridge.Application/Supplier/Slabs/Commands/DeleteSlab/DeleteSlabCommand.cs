using MediatR;

namespace StoneBridge.Application.Supplier.Slabs.Commands.DeleteSlab;

public sealed record DeleteSlabCommand(Guid SlabId) : IRequest;
