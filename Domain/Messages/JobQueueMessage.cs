namespace Company.Function.Domain.Messages;

public sealed class JobQueueMessage
{
    public Guid Id { get; init; }
    public string Status { get; init; } = default!;
}
