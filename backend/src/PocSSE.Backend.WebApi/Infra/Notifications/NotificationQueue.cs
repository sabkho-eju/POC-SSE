using System.Collections.Concurrent;
using System.Threading.Channels;

namespace PocSSE.Backend.WebApi.Infra.Notifications
{
    public class NotificationQueue
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<QueuedNotification>>> _subscriptions = new();

        public (Guid subscriptionId, ChannelReader<QueuedNotification> reader) Subscribe(string clientId)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<QueuedNotification>();
            var connections = _subscriptions.GetOrAdd(clientId, _ => new ConcurrentDictionary<Guid, Channel<QueuedNotification>>());
            connections[id] = channel;

            return (id, channel.Reader);
        }

        public void Unsubscribe(string clientId, Guid subscriptionId)
        {
            if (!_subscriptions.TryGetValue(clientId, out var connections))
            {
                return;
            }

            if (connections.TryRemove(subscriptionId, out var channel))
            {
                channel.Writer.TryComplete();
            }

            if (connections.IsEmpty)
            {
                _subscriptions.TryRemove(clientId, out _);
            }
        }

        public bool PublishToClient(string clientId, QueuedNotification notification)
        {
            if (!_subscriptions.TryGetValue(clientId, out var connections))
            {
                return false;
            }

            foreach (var channel in connections.Values)
            {
                channel.Writer.TryWrite(notification);
            }

            return true;
        }
    }
}
