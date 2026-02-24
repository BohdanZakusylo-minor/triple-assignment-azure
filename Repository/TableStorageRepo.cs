using Azure.Data.Tables;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Entities;
using Company.Function.Domain.Enums;
using Company.Function.Infrastructure;

namespace Company.Function.Repository;

public sealed class ProcessRecordRepository : IProcessRecordRepository
{
    private readonly TableClient _table;

    public ProcessRecordRepository(TableClient table)
    {
        _table = table;
    }

    public async Task AddAsync(JobEntity record, CancellationToken ct = default)
    {
        const string partitionKey = "jobs";
        var rowKey = record.Id.ToString("N");


        Console.WriteLine($"[TABLE] Name={_table.Name}");
        Console.WriteLine($"[TABLE] Uri ={_table.Uri}");

        var entity = new JobTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Id = record.Id,
            Status = record.Status.ToString()
        };

        await _table.CreateIfNotExistsAsync(ct);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

        var found = _table.Query<TableEntity>(x => x.PartitionKey == "jobs").Take(10).ToList();

        Console.WriteLine($"Found {found.Count} entities in partition 'jobs' on table {_table.Name} at {_table.Uri}");
        foreach (var e in found)
        {
            Console.WriteLine($"PK={e.PartitionKey}, RK={e.RowKey}");
        }
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
}