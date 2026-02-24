namespace Company.Function.Domain.Messages;

public sealed class StationImageJobMessage
{
    public string StationId { get; init; } = default!;
    public string StationName { get; init; } = default!;
    public string ImageUrl { get; init; } = default!;
}