using System.Text.Json.Nodes;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Common.Interfaces;

public interface ISupplierProfileRepository
{
    Task<SupplierProfileDto?> GetProfileAsync(Guid tenantId, CancellationToken ct = default);

    Task<SupplierProfileDto> UpsertProfileAsync(Guid tenantId, UpdateProfileRequest request, CancellationToken ct = default);

    Task<SupplierStatsDto?> GetStatsAsync(Guid tenantId, CancellationToken ct = default);

    Task<JsonObject> GetNotificationPrefsAsync(Guid tenantId, CancellationToken ct = default);

    Task<JsonObject> UpdateNotificationPrefsAsync(Guid tenantId, JsonObject prefs, CancellationToken ct = default);
}
