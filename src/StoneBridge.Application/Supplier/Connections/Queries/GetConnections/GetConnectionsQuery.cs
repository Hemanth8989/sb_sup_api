using MediatR;
using StoneBridge.Application.Supplier.Connections.DTOs;

namespace StoneBridge.Application.Supplier.Connections.Queries.GetConnections;

public sealed record GetConnectionsQuery(string? Status) : IRequest<IReadOnlyList<ConnectionDto>>;
