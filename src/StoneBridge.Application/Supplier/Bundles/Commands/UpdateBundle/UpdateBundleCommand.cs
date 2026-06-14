using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Commands.UpdateBundle;

public sealed record UpdateBundleCommand(Guid BundleId, UpdateBundleRequest Request) : IRequest<BundleDto>;
