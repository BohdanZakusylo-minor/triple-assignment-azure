using Azure.Data.Tables;
using Company.Function.Domain.Entities;
using Company.Function.Domain.Enums;
using Company.Function.Infrastrucure.Interfaces;
using Company.Function.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Company.Function.Repository;

public sealed class ProcessRecordRepository : IProcessRecordRepository
{
    private readonly TableClient _table;
    private readonly ILogger<ProcessRecordRepository> _logger;

    public ProcessRecordRepository(TableClient table, ILogger<ProcessRecordRepository> logger)
    {
        _table = table;
        _logger = logger;
    }

    public async Task AddAsync(JobEntity record, CancellationToken ct = default)
    {
        const string partitionKey = "jobs";
        var rowKey = record.Id.ToString("N");

        var entity = new JobTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Id = record.Id,
            Status = record.Status.ToString()
        };

        await _table.CreateIfNotExistsAsync(ct);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        _logger.LogDebug("Upserted job {JobId} to table {TableName}", record.Id, _table.Name);
    }

    public async Task UpsertParentStartedAsync(Guid jobId, int total, CancellationToken ct)
    {
        await _table.CreateIfNotExistsAsync(ct);

        var entity = new JobTableEntity
        {
            PartitionKey = "jobs",
            RowKey = jobId.ToString("N"),
            Id = jobId,
            Status = JobStatusEnum.STARTED.ToString(),
            Total = total,
            Completed = 0
        };

        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task IncrementCompletedAndMaybeFinishAsync(Guid jobId, CancellationToken ct)
    {
        const string pk = "jobs";
        var rk = jobId.ToString("N");

        while (true)
        {
            var current = await _table.GetEntityAsync<JobTableEntity>(pk, rk, cancellationToken: ct);
            var entity = current.Value;

            entity.Completed += 1;
            if (entity.Completed >= entity.Total)
                entity.Status = JobStatusEnum.FINISHED.ToString();

            try
            {
                await _table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace, ct);
                return;
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 412)
            {
            }
        }
    }

    public async Task<JobStatusResult?> GetByJobIdAsync(Guid jobId, CancellationToken ct = default)
    {
        const string pk = "jobs";
        var rk = jobId.ToString("N");
        var response = await _table.GetEntityIfExistsAsync<JobTableEntity>(pk, rk, cancellationToken: ct);
        if (!response.HasValue)
            return null;
        var e = response.Value;
        return new JobStatusResult(e.Id, e.Status ?? string.Empty, e.Total, e.Completed);
    }
}