using System.Text.Json;
using Company.Function.Domain.Enums;
using Company.Function.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Company.Function.Domain.Entities;

namespace Company.Function.Routers.QueueWorkers;

public sealed class JobRequestQueueTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly ILogger<JobRequestQueueTrigger> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JobRequestQueueTrigger(
        IProcessRecordRepository repository,
        ILogger<JobRequestQueueTrigger> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Function("ProcessJobRequest")]
    public async Task Run(
        [QueueTrigger("imagequeue", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Raw message: {Message}", message);

        var parts = message.Split('|');

        if (parts.Length != 2)
            throw new InvalidOperationException("Invalid message format");

        if (!Guid.TryParse(parts[0], out var jobId))
            throw new InvalidOperationException("Invalid Guid");

        if (!Enum.TryParse<JobStatusEnum>(parts[1], true, out var status))
            throw new InvalidOperationException("Invalid Status");

        var entity = new JobEntity
        {
            Id = jobId,
            Status = status
        };

        await _repository.AddAsync(entity, cancellationToken);

        _logger.LogInformation("Persisted job {JobId} with status {Status}", entity.Id, entity.Status);
    }

}