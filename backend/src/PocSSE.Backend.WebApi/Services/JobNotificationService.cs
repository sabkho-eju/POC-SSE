using System.Collections.Concurrent;
using System.Threading.Channels;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Services
{
    public sealed class JobNotificationService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<JobNotification>>> _subscriptions = new();

        public (Guid subscriptionId, ChannelReader<JobNotification> reader) Subscribe(string clientId)
        {
            var id = Guid.NewGuid();
            var channel = Channel.CreateUnbounded<JobNotification>();
            var connections = _subscriptions.GetOrAdd(clientId, _ => new ConcurrentDictionary<Guid, Channel<JobNotification>>());
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

        public void PublishToClient(string clientId, JobNotification notification)
        {
            if (!_subscriptions.TryGetValue(clientId, out var connections))
            {
                return;
            }

            foreach (var channel in connections.Values)
            {
                channel.Writer.TryWrite(notification);
            }
        }

        public void Broadcast(JobNotification notification)
        {
            foreach (var connectionSet in _subscriptions.Values)
            {
                foreach (var channel in connectionSet.Values)
                {
                    channel.Writer.TryWrite(notification);
                }
            }
        }
    }
}
