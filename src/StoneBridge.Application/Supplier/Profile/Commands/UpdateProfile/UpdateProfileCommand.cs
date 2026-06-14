using MediatR;
using StoneBridge.Application.Supplier.Profile.DTOs;

namespace StoneBridge.Application.Supplier.Profile.Commands.UpdateProfile;

public sealed record UpdateProfileCommand(UpdateProfileRequest Request) : IRequest<SupplierProfileDto>;
