using Azure;
using Azure.Data.Tables;

namespace Company.Function.Infrastructure;

public sealed class JobTableEntity : ITableEntity
{
    public string PartitionKey { get; set; } = default!;
    public string RowKey { get; set; } = default!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public Guid Id { get; set; }
    public string Status { get; set; } = default!;
    public int Total { get; set; }
    public int Completed { get; set; }
}