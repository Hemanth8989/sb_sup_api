namespace StoneBridge.Application.Supplier.Connections.DTOs;

public sealed record ConnectionDto
{
    public Guid     Id               { get; init; }
    public Guid     FabricatorId     { get; init; }
    public string   FabricatorName   { get; init; } = string.Empty;
    public string   FabricatorSlug   { get; init; } = string.Empty;
    public Guid     SupplierId       { get; init; }
    public string   Status           { get; init; } = string.Empty;
    public string   PricingTier      { get; init; } = "standard";
    public string?  RequestMessage   { get; init; }
    public string?  DeclineReason    { get; init; }
    public string?  FabricatorNotes  { get; init; }
    public DateTime RequestedAt      { get; init; }
    public DateTime? ConnectedAt     { get; init; }
    public DateTime? SuspendedAt     { get; init; }
    public DateTime? TerminatedAt    { get; init; }
    public DateTime UpdatedAt        { get; init; }
    // Fabricator profile summary
    public string?  FabricatorCity   { get; init; }
    public string?  FabricatorState  { get; init; }
    public string?  FabricatorPhone  { get; init; }
    public string?  AssignedPriceListId   { get; init; }
    public string?  AssignedPriceListName { get; init; }
}

public sealed record UpdateConnectionTierRequest(string PricingTier);
public sealed record UpdateConnectionNotesRequest(string? FabricatorNotes);
public sealed record RespondConnectionRequest(string Action, string? Reason);
