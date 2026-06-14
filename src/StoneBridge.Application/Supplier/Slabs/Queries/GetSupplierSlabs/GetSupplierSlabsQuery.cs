using MediatR;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Queries.GetSupplierSlabs;

/// <summary>
/// Query: supplier views their own slab inventory.
/// Returns all slabs belonging to the authenticated supplier tenant,
/// filterable by status, material, finish, thickness, remnant flag, and warehouse.
/// Results are paginated.
///
/// Authorization: only supplier tenants can execute this query.
/// </summary>
public sealed record GetSupplierSlabsQuery(SupplierSlabFilterParams FilterParams)
    : IRequest<PagedResult<SupplierSlabDto>>;
