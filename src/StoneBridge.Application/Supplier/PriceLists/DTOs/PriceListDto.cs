namespace StoneBridge.Application.Supplier.PriceLists.DTOs;

public record PriceListDto
{
    public Guid     Id         { get; init; }
    public string   Name       { get; init; } = string.Empty;
    public string   Tier       { get; init; } = "standard";
    public string   Currency   { get; init; } = "USD";
    public DateOnly? ValidFrom  { get; init; }
    public DateOnly? ValidTo    { get; init; }
    public bool     IsActive   { get; init; }
    public int      ItemCount  { get; init; }
    public DateTime CreatedAt  { get; init; }
    public DateTime UpdatedAt  { get; init; }
}

public record PriceListDetailDto : PriceListDto
{
    public IReadOnlyList<PriceListItemDto> Items { get; init; } = [];
}

public sealed record PriceListItemDto
{
    public Guid    Id           { get; init; }
    public Guid    VariantId    { get; init; }
    public string  VariantName  { get; init; } = string.Empty;
    public string  Sku          { get; init; } = string.Empty;
    public decimal UnitPrice    { get; init; }
    public string  Currency     { get; init; } = "USD";
}

public sealed record CreatePriceListRequest(
    string   Name,
    string   Tier,
    string?  Currency,
    DateOnly? ValidFrom,
    DateOnly? ValidTo
);

public sealed record UpdatePriceListRequest(
    string   Name,
    string   Tier,
    string?  Currency,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool     IsActive
);

public sealed record UpsertPriceListItemRequest(
    Guid    VariantId,
    decimal UnitPrice
);
