using System.Text.Json;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.QueueWorkers;

public sealed class JobRequestQueueTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<JobRequestQueueTrigger> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobRequestQueueTrigger(
        IProcessRecordRepository repository,
        IHttpClientFactory httpFactory,
        ILogger<JobRequestQueueTrigger> logger)
    {
        _repository = repository;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    [Function("ProcessStationJob")]
    public async Task Run(
        [QueueTrigger("imagequeue", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken ct)
    {
        var job = JsonSerializer.Deserialize<StationJobMessage>(message, JsonOptions)
                  ?? throw new InvalidOperationException("Invalid StationJobMessage JSON.");

        // Fetch image
        var http = _httpFactory.CreateClient("images");
        var bytes = await http.GetByteArrayAsync(job.ImageUrl, ct);

        _logger.LogInformation("Fetched image for {Station} bytes={Bytes}", job.StationName, bytes.Length);

        // Mark progress + maybe finish parent job
        await _repository.IncrementCompletedAndMaybeFinishAsync(job.ParentJobId, ct);
    }
}