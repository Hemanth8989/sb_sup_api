using System.Text.Json.Nodes;
using MediatR;

namespace StoneBridge.Application.Supplier.Profile.Queries.GetNotificationPrefs;

public sealed record GetNotificationPrefsQuery : IRequest<JsonObject>;
