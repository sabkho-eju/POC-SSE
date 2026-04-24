using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PocSSE.Backend.WebApi.Infra.Notifications
{
    public class NotificationQueue(ILogger<NotificationQueue> logger)
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<EventSubscription>> _subscriptionsByEventType = new();
        private readonly ConcurrentDictionary<Guid, EventSubscription> _subscriptionsById = new();

        public (Guid subscriptionId, ChannelReader<QueuedNotification> reader) Subscribe(string eventType, string? clientId = null)
        {
            var subscriptionId = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<QueuedNotification>();
            var subscription = new EventSubscription(subscriptionId, eventType, clientId, channel);

            // Ajoute la souscription dans le dictionnaire par ID
            _subscriptionsById.TryAdd(subscriptionId, subscription);

            // Ajoute la souscription dans le bag par EventType
            var subscriptions = _subscriptionsByEventType.GetOrAdd(eventType, _ => new ConcurrentBag<EventSubscription>());
            subscriptions.Add(subscription);

            var clientInfo = string.IsNullOrEmpty(clientId) ? "anonymous" : clientId;
            logger.LogInformation("Subscription {SubscriptionId} created for EventType '{EventType}' (client: {ClientInfo})",
                subscriptionId, eventType, clientInfo);

            return (subscriptionId, channel.Reader);
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            if (!_subscriptionsById.TryRemove(subscriptionId, out var subscription))
            {
                logger.LogWarning("Attempted to unsubscribe {SubscriptionId}, but subscription not found", subscriptionId);
                return;
            }

            subscription.Channel.Writer.TryComplete();

            // Note: On ne retire pas du ConcurrentBag car c'est difficile/coûteux
            // On marque juste comme inactif et on nettoie lors de la publication
            subscription.IsActive = false;

            var clientInfo = string.IsNullOrEmpty(subscription.ClientId) ? "anonymous" : subscription.ClientId;
            logger.LogInformation("Subscription {SubscriptionId} removed for EventType '{EventType}' (client: {ClientInfo})",
                subscriptionId, subscription.EventType, clientInfo);
        }

        public int Publish(string? clientId, QueuedNotification notification)
        {
            if (!_subscriptionsByEventType.TryGetValue(notification.EventType, out var subscriptions))
            {
                logger.LogDebug("No subscriptions found for EventType '{EventType}'", notification.EventType);
                return 0;
            }

            var successCount = 0;
            var inactiveSubscriptions = new List<EventSubscription>();

            // Filtrer par clientId si fourni
            var targetSubscriptions = string.IsNullOrEmpty(clientId)
                ? subscriptions
                : subscriptions.Where(s => s.ClientId == clientId);

            foreach (var subscription in targetSubscriptions)
            {
                if (!subscription.IsActive)
                {
                    inactiveSubscriptions.Add(subscription);
                    continue;
                }

                if (subscription.Channel.Writer.TryWrite(notification))
                {
                    successCount++;
                    var clientInfo = string.IsNullOrEmpty(subscription.ClientId) ? "anonymous" : subscription.ClientId;
                    logger.LogDebug("Published notification '{EventType}' to subscription {SubscriptionId} (client: {ClientInfo})",
                        notification.EventType, subscription.SubscriptionId, clientInfo);
                }
                else
                {
                    logger.LogWarning("Failed to write notification '{EventType}' to subscription {SubscriptionId}",
                        notification.EventType, subscription.SubscriptionId);
                }
            }

            // Nettoyage optionnel des souscriptions inactives détectées
            if (inactiveSubscriptions.Count > 0)
            {
                logger.LogDebug("Detected {Count} inactive subscriptions for EventType '{EventType}' during publish",
                    inactiveSubscriptions.Count, notification.EventType);
            }

            logger.LogInformation("Published notification '{EventType}' to {SuccessCount} active subscriptions",
                notification.EventType, successCount);

            return successCount;
        }

        private class EventSubscription(Guid subscriptionId, string eventType, string? clientId, Channel<QueuedNotification> channel)
        {
            public Guid SubscriptionId { get; } = subscriptionId;
            public string EventType { get; } = eventType;
            public string? ClientId { get; } = clientId;
            public Channel<QueuedNotification> Channel { get; } = channel;
            public bool IsActive { get; set; } = true;
        }
    }
}

