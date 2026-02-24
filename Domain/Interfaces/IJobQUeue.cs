namespace Company.Function.Domain.Interfaces;

public interface IJobQueue
{
    Task EnqueueAsync(string message, CancellationToken ct);
}