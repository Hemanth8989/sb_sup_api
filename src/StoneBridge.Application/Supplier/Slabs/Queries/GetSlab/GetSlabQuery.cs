using MediatR;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Queries.GetSlab;

public sealed record GetSlabQuery(Guid SlabId) : IRequest<SupplierSlabDto?>;
