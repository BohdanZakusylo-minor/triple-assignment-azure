using Company.Function.Domain.Enums;

namespace Company.Function.Domain.Entities;

public sealed class JobEntity
{
    public Guid Id { get; init; }
    public JobStatusEnum Status { get; init; }
}