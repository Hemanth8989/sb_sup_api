using MediatR;
using Microsoft.Extensions.Logging;
using StoneBridge.Application.Common.Exceptions;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Common.Models;
using StoneBridge.Application.Supplier.Slabs.DTOs;

namespace StoneBridge.Application.Supplier.Slabs.Queries.GetSupplierSlabs;

/// <summary>
/// Handles GetSupplierSlabsQuery.
/// Authorization: supplier tenants only — fabricators cannot view another supplier's inventory.
/// Delegates all data access to ISupplierSlabRepository.
/// </summary>
public sealed class GetSupplierSlabsQueryHandler
    : IRequestHandler<GetSupplierSlabsQuery, PagedResult<SupplierSlabDto>>
{
    private readonly ISupplierSlabRepository                  _repository;
    private readonly ICurrentTenant                           _currentTenant;
    private readonly ILogger<GetSupplierSlabsQueryHandler>    _logger;

    public GetSupplierSlabsQueryHandler(
        ISupplierSlabRepository                 repository,
        ICurrentTenant                          currentTenant,
        ILogger<GetSupplierSlabsQueryHandler>   logger)
    {
        _repository    = repository;
        _currentTenant = currentTenant;
        _logger        = logger;
    }

    public async Task<PagedResult<SupplierSlabDto>> Handle(
        GetSupplierSlabsQuery request,
        CancellationToken     cancellationToken)
    {
        if (!_currentTenant.IsSupplier)
        {
            throw new ForbiddenException(
                "Inventory management is only accessible to supplier tenants. " +
                "Fabricators should use /api/v1/catalog/slabs to browse available stock.");
        }

        var result = await _repository.GetInventoryAsync(
            supplierId:   _currentTenant.TenantId,
            filterParams: request.FilterParams,
            ct:           cancellationToken);

        _logger.LogDebug(
            "Supplier inventory returned {Count} of {Total} slabs for supplier {SupplierId}",
            result.Items.Count,
            result.TotalCount,
            _currentTenant.TenantId);

        return result;
    }
}
