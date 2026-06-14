using StoneBridge.Application.Supplier.Warehouses.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IWarehouseRepository
{
    Task<IReadOnlyList<WarehouseDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<WarehouseDto?> GetByIdAsync(Guid tenantId, Guid warehouseId, CancellationToken ct = default);
    Task<WarehouseDto> CreateAsync(Guid tenantId, CreateWarehouseRequest request, CancellationToken ct = default);
    Task<WarehouseDto> UpdateAsync(Guid tenantId, Guid warehouseId, UpdateWarehouseRequest request, CancellationToken ct = default);
    Task SetPrimaryAsync(Guid tenantId, Guid warehouseId, CancellationToken ct = default);
    Task DeactivateAsync(Guid tenantId, Guid warehouseId, CancellationToken ct = default);
    Task<int> TransferSlabsAsync(Guid tenantId, IEnumerable<Guid> slabIds, Guid targetWarehouseId, string? rackLocation, CancellationToken ct = default);
}

public sealed record CreateWarehouseRequest(
    string  Name,
    string? AddressLine1,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    string? Phone,
    bool    SetAsPrimary
);

public sealed record UpdateWarehouseRequest(
    string  Name,
    string? AddressLine1,
    string? City,
    string? StateProvince,
    string? PostalCode,
    string? Country,
    string? Phone
);

public sealed record TransferSlabsRequest(
    IReadOnlyList<Guid> SlabIds,
    Guid                TargetWarehouseId,
    string?             RackLocation
);
