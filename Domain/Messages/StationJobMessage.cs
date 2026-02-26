namespace Company.Function.Domain.Messages;

public sealed class StationJobMessage
{
    public Guid ParentJobId { get; init; }
    public string StationId { get; init; } = default!;
    public string StationName { get; init; } = default!;
    public string ImageUrl { get; init; } = default!;
}