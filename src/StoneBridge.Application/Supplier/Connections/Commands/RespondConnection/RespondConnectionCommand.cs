using MediatR;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.RespondConnection;

public sealed record RespondConnectionCommand(
    Guid   ConnectionId,
    string Action,
    string? Reason
) : IRequest<ConnectionDto>;
