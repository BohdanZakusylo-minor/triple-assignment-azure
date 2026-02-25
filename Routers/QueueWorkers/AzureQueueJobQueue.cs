using Company.Function.Domain.Interfaces;
using Azure.Storage.Queues;

namespace Company.Function.Infrastructure.Queues;

public sealed class AzureQueueJobQueue : IJobQueue
{
    private readonly QueueClient _queue;

    public AzureQueueJobQueue(QueueClient queue) => _queue = queue;

    public Task EnqueueAsync(string message, CancellationToken ct)
        => _queue.SendMessageAsync(message, ct);
}