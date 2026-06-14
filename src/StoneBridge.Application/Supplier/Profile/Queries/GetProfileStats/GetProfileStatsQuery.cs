using MediatR;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetProfileStats;

public sealed record GetProfileStatsQuery : IRequest<SupplierStatsDto>;
