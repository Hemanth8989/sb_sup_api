using MediatR;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.UpdateConnectionTier;

public sealed record UpdateConnectionTierCommand(Guid ConnectionId, string Tier) : IRequest<ConnectionDto>;
