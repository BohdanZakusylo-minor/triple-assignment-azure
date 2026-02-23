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
        [QueueTrigger("imagequeue")] string message,
        CancellationToken cancellationToken)
    {

        var entity = new JobEntity
        {
            Id = new Guid(),
            Status = JobStatusEnum.STARTED
        };

        await _repository.AddAsync(entity, cancellationToken);

        _logger.LogInformation("Persisted job {JobId} with status {Status}", entity.Id, entity.Status);
    }

}