using MediatR;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Commands.UpdateNotes;

public sealed record UpdateNotesCommand(Guid ConnectionId, string? Notes) : IRequest<ConnectionDto>;
