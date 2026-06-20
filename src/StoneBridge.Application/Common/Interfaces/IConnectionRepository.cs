using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IConnectionRepository
{
    Task<IReadOnlyList<ConnectionDto>> GetAllAsync(Guid supplierId, string? status, CancellationToken ct = default);
    Task<ConnectionDto?> GetByIdAsync(Guid supplierId, Guid connectionId, CancellationToken ct = default);
    Task<ConnectionDto> RespondAsync(Guid supplierId, Guid connectionId, string action, string? reason, CancellationToken ct = default);
    Task<ConnectionDto> UpdateTierAsync(Guid supplierId, Guid connectionId, string tier, CancellationToken ct = default);
    Task<ConnectionDto> UpdateNotesAsync(Guid supplierId, Guid connectionId, string? notes, CancellationToken ct = default);
    Task AssignPriceListAsync(Guid supplierId, Guid connectionId, Guid priceListId, Guid assignedBy, CancellationToken ct = default);
    Task RemovePriceListAsync(Guid supplierId, Guid connectionId, CancellationToken ct = default);
}
