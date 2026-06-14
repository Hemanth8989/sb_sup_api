using MediatR;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Queries.GetBundles;

public sealed record GetBundlesQuery(string? Search = null) : IRequest<IReadOnlyList<BundleDto>>;
