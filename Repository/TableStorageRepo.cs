using Azure.Data.Tables;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Entities;
using Company.Function.Infrastructure;

namespace Company.Function.Infrastructure.TableStorage;

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

        var entity = new JobTableEntity
        {
            PartitionKey = partitionKey,
            RowKey = rowKey,
            Id = record.Id,
            Status = record.Status.ToString()
        };

        await _table.CreateIfNotExistsAsync(ct);
        await _table.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }
}