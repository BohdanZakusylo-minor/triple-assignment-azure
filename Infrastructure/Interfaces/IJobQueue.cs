namespace Company.Function.Infrastrucure.Interfaces;

public interface IJobQueue
{
    Task EnqueueAsync(string message, CancellationToken ct);
}
