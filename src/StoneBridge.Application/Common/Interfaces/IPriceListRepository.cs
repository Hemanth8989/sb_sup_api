using StoneBridge.Application.Supplier.PriceLists.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface IPriceListRepository
{
    Task<IReadOnlyList<PriceListDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default);
    Task<PriceListDetailDto?> GetByIdAsync(Guid tenantId, Guid priceListId, CancellationToken ct = default);
    Task<PriceListDto> CreateAsync(Guid tenantId, CreatePriceListRequest req, CancellationToken ct = default);
    Task<PriceListDto> UpdateAsync(Guid tenantId, Guid priceListId, UpdatePriceListRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid tenantId, Guid priceListId, CancellationToken ct = default);
    Task<PriceListItemDto> UpsertItemAsync(Guid tenantId, Guid priceListId, UpsertPriceListItemRequest req, CancellationToken ct = default);
    Task RemoveItemAsync(Guid tenantId, Guid priceListId, Guid itemId, CancellationToken ct = default);
    Task<PriceListDto> CloneAsync(Guid tenantId, Guid priceListId, string newName, CancellationToken ct = default);
}
