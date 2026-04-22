using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PocSSE.Backend.WebApi.Infra.Notifications
{
    public class NotificationQueue(ILogger<NotificationQueue> logger)
    {
        private readonly ConcurrentDictionary<string, ClientSubscription> _subscriptions = new();

        public (Guid subscriptionId, ChannelReader<QueuedNotification> reader) Subscribe(string clientId)
        {
            // Vérifie si une souscription existe déjà
            if (_subscriptions.TryGetValue(clientId, out var existingSubscription))
            {
                logger.LogWarning("Client {ClientId} attempted to subscribe but already has an active subscription {SubscriptionId}. Returning existing subscription.",
                    clientId, existingSubscription.SubscriptionId);
                return (existingSubscription.SubscriptionId, existingSubscription.Channel.Reader);
            }

            // Crée une nouvelle souscription
            var subscriptionId = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<QueuedNotification>();
            var newSubscription = new ClientSubscription(subscriptionId, channel);

            // Ajoute la souscription (ne devrait jamais échouer vu le check au-dessus)
            if (_subscriptions.TryAdd(clientId, newSubscription))
            {
                logger.LogInformation("Client {ClientId} subscribed with subscription {SubscriptionId}", clientId, subscriptionId);
                return (subscriptionId, channel.Reader);
            }

            // Si TryAdd échoue (race condition), retourne l'existante
            var actualSubscription = _subscriptions[clientId];
            logger.LogWarning("Client {ClientId} subscription race condition detected. Returning existing subscription {SubscriptionId}",
                clientId, actualSubscription.SubscriptionId);
            return (actualSubscription.SubscriptionId, actualSubscription.Channel.Reader);
        }

        public void Unsubscribe(string clientId, Guid subscriptionId)
        {
            if (!_subscriptions.TryGetValue(clientId, out var subscription))
            {
                logger.LogWarning("Attempted to unsubscribe client {ClientId} with subscription {SubscriptionId}, but client not found",
                    clientId, subscriptionId);
                return;
            }

            // Vérifie que c'est bien la bonne souscription avant de la supprimer
            if (subscription.SubscriptionId == subscriptionId)
            {
                if (_subscriptions.TryRemove(clientId, out var removedSubscription))
                {
                    removedSubscription.Channel.Writer.TryComplete();
                    logger.LogInformation("Client {ClientId} unsubscribed {SubscriptionId}", clientId, subscriptionId);
                }
            }
            else
            {
                logger.LogWarning("Client {ClientId} attempted to unsubscribe {SubscriptionId}, but current subscription is {CurrentSubscriptionId}",
                    clientId, subscriptionId, subscription.SubscriptionId);
            }
        }

        public bool PublishToClient(string clientId, QueuedNotification notification)
        {
            if (!_subscriptions.TryGetValue(clientId, out var subscription))
            {
                logger.LogDebug("No subscription found for client {ClientId}, notification '{EventName}' not sent",
                    clientId, notification.EventName);
                return false;
            }

            if (subscription.Channel.Writer.TryWrite(notification))
            {
                logger.LogInformation("Published notification '{EventName}' to client {ClientId} (subscription {SubscriptionId})",
                    notification.EventName, clientId, subscription.SubscriptionId);
                return true;
            }
            else
            {
                logger.LogWarning("Failed to write notification '{EventName}' to client {ClientId} (subscription {SubscriptionId})",
                    notification.EventName, clientId, subscription.SubscriptionId);
                return false;
            }
        }

        private record ClientSubscription(Guid SubscriptionId, Channel<QueuedNotification> Channel);
    }
}
