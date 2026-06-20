using MediatR;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Queries.GetConnection;

public sealed record GetConnectionQuery(Guid ConnectionId) : IRequest<ConnectionDto?>;
