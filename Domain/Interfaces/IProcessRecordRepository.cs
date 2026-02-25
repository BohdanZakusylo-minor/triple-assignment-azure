using Company.Function.Domain.Entities;

namespace Company.Function.Domain.Interfaces;

public interface IProcessRecordRepository
{
    Task AddAsync(JobEntity record, CancellationToken ct = default);
    Task UpsertParentStartedAsync(Guid jobId, int total, CancellationToken ct);
    Task IncrementCompletedAndMaybeFinishAsync(Guid jobId, CancellationToken ct);
    Task<JobStatusResult?> GetByJobIdAsync(Guid jobId, CancellationToken ct = default);
}

public sealed record JobStatusResult(Guid JobId, string Status, int Total, int Completed);
