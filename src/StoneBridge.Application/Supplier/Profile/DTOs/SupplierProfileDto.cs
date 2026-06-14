using System.Text.Json.Nodes;

namespace StoneBridge.Application.Supplier.Profile.DTOs;

public sealed record SupplierProfileDto
{
    public Guid        TenantId         { get; init; }
    public string      DisplayName      { get; init; } = string.Empty;
    public string?     LogoUrl          { get; init; }
    public string?     Description      { get; init; }
    public string?     Website          { get; init; }
    public string?     Phone            { get; init; }
    public string?     AddressLine1     { get; init; }
    public string?     AddressLine2     { get; init; }
    public string?     City             { get; init; }
    public string?     StateProvince    { get; init; }
    public string?     PostalCode       { get; init; }
    public string      Country          { get; init; } = "US";
    public int?        EstablishedYear  { get; init; }
    public bool        Verified         { get; init; }
    public DateTime?   VerifiedAt       { get; init; }
    public JsonObject  NotificationPrefs { get; init; } = [];
    public DateTime    UpdatedAt        { get; init; }
}
