using Company.Function.Application;
using Company.Function.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.QueueWorkers;

public sealed class BuienradarFanoutTrigger
{
    private const int MaxStationsPerJob = 50;

    private readonly IProcessRecordRepository _repository;
    private readonly StationFanOutService _fanOutService;
    private readonly ILogger<BuienradarFanoutTrigger> _logger;

    public BuienradarFanoutTrigger(
        IProcessRecordRepository repository,
        StationFanOutService fanOutService,
        ILogger<BuienradarFanoutTrigger> logger)
    {
        _repository = repository;
        _fanOutService = fanOutService;
        _logger = logger;
    }

    [Function("BuienradarFanout")]
    public async Task Run(
        [QueueTrigger("fanout-start", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken ct)
    {
        message = message?.Trim() ?? "";

        if (!Guid.TryParse(message, out var parentJobId))
            throw new InvalidOperationException($"Invalid parentJobId in fanout-start message: '{message}'");

        _logger.LogInformation("Fanout start: ParentJobId={ParentJobId}", parentJobId);

        var queued = await _fanOutService.FanOutAsync(parentJobId, MaxStationsPerJob, ct);
        await _repository.UpsertParentStartedAsync(parentJobId, queued, ct);

        _logger.LogInformation("Fanout done: ParentJobId={ParentJobId}, queued={Queued}", parentJobId, queued);
    }
}