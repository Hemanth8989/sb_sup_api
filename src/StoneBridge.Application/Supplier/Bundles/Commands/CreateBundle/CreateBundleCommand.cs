using MediatR;
using StoneBridge.Application.Common.Interfaces;
using StoneBridge.Application.Supplier.Bundles.DTOs;

namespace StoneBridge.Application.Supplier.Bundles.Commands.CreateBundle;

public sealed record CreateBundleCommand(CreateBundleRequest Request) : IRequest<BundleDto>;
