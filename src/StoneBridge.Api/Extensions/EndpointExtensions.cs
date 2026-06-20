using StoneBridge.Api.Endpoints;

namespace StoneBridge.Api.Extensions;

/// <summary>
/// Registers all API endpoint groups.
/// Add one line per feature area as the API grows.
/// </summary>
public static class EndpointExtensions
{
    public static WebApplication MapStoneBridgeEndpoints(this WebApplication app)
    {
        // Infrastructure endpoints — always present, no auth required
        app.MapHealthEndpoints();

        // Feature endpoints
        app.MapCatalogEndpoints();
        app.MapSupplierSlabEndpoints();
        app.MapSupplierProfileEndpoints();
        app.MapWarehouseEndpoints();
        app.MapBundleEndpoints();
        app.MapPurchaseOrderEndpoints();
        app.MapConnectionEndpoints();
        app.MapPriceListEndpoints();
        app.MapProductInventoryEndpoints();
        app.MapAnalyticsEndpoints();

        return app;
    }
}