using Company.Function.Domain.Entities;

namespace Company.Function.Domain.Interfaces;

public interface IProcessRecordRepository
{
    Task AddAsync(JobEntity record, CancellationToken ct = default);
}