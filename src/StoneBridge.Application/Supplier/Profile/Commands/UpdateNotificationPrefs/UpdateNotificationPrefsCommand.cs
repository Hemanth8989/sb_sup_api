using System.Text.Json.Nodes;
using MediatR;

namespace StoneBridge.Application.Supplier.Profile.Commands.UpdateNotificationPrefs;

public sealed record UpdateNotificationPrefsCommand(JsonObject Prefs) : IRequest<JsonObject>;
