using MediatR;
using StoneBridge.Application.Common.Interfaces;

namespace StoneBridge.Application.Supplier.Warehouses.Queries.ExportWarehouseSlabs;

internal sealed class ExportWarehouseSlabsQueryHandler : IRequestHandler<ExportWarehouseSlabsQuery, string>
{
    private readonly ICurrentTenant       _tenant;
    private readonly IWarehouseRepository _repo;

    public ExportWarehouseSlabsQueryHandler(ICurrentTenant tenant, IWarehouseRepository repo)
    {
        _tenant = tenant;
        _repo   = repo;
    }

    public Task<string> Handle(ExportWarehouseSlabsQuery request, CancellationToken cancellationToken)
        => _repo.ExportSlabsAsCsvAsync(_tenant.TenantId, request.WarehouseId, cancellationToken);
}
