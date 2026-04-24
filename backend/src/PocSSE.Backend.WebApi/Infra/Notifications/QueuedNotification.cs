using System.Text.Json;

namespace PocSSE.Backend.WebApi.Infra.Notifications;

public sealed record QueuedNotification(string EventType, JsonElement? Data = null);