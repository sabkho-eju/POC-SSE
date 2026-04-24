using System.Threading.Channels;
using PocSSE.Backend.WebApi.Models.Entities;

namespace PocSSE.Backend.WebApi.Infra.Jobs
{
    public sealed class BackgroundJobQueue
    {
        private readonly Channel<QueuedJob> _queue = Channel.CreateUnbounded<QueuedJob>();

        public ValueTask QueueAsync(QueuedJob job, CancellationToken cancellationToken = default)
            => _queue.Writer.WriteAsync(job, cancellationToken);

        public ValueTask<QueuedJob> DequeueAsync(CancellationToken cancellationToken)
            => _queue.Reader.ReadAsync(cancellationToken);
    }
}

