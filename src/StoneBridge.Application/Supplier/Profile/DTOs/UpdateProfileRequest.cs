namespace StoneBridge.Application.Supplier.Profile.DTOs;

public sealed record UpdateProfileRequest
{
    public string  DisplayName      { get; init; } = string.Empty;
    public string? Description      { get; init; }
    public string? Website          { get; init; }
    public string? Phone            { get; init; }
    public string? AddressLine1     { get; init; }
    public string? AddressLine2     { get; init; }
    public string? City             { get; init; }
    public string? StateProvince    { get; init; }
    public string? PostalCode       { get; init; }
    public string? Country          { get; init; }
    public int?    EstablishedYear  { get; init; }
}
