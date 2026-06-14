using MediatR;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Queries.GetBundle;

public sealed record GetBundleQuery(Guid BundleId) : IRequest<BundleDetailDto?>;
