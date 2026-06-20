using MediatR;

namespace StoneBridge.Application.Supplier.Slabs.Commands.BulkUpdateSlabStatus;

public sealed record BulkUpdateSlabStatusCommand(IReadOnlyList<Guid> SlabIds, string Status) : IRequest<int>;
