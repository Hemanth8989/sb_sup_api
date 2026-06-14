using MediatR;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetProfile;

public sealed record GetProfileQuery : IRequest<SupplierProfileDto>;
