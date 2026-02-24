using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Company.Function.Domain.Messages;
using Company.Function.Domain.Interfaces;

namespace Company.Function.Routers.QueueWorkers;

public sealed class JobRequestQueueTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IHttpClientFactory _httpFactory;
    private readonly Azure.Storage.Queues.QueueClient _queueClient;
    private readonly ILogger<JobRequestQueueTrigger> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobRequestQueueTrigger(
        IProcessRecordRepository repository,
        IHttpClientFactory httpFactory,
        Azure.Storage.Queues.QueueClient queueClient,
        ILogger<JobRequestQueueTrigger> logger)
    {
        _repository = repository;
        _httpFactory = httpFactory;
        _queueClient = queueClient;
        _logger = logger;
    }

    [Function("ProcessJobRequest")]
    public async Task Run(
        [QueueTrigger("imagequeue", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken ct)
    {
        message = message?.Trim() ?? "";

        if (Guid.TryParse(message, out var parentJobId))
        {
            await HandleFanoutStart(parentJobId, ct);
            return;
        }

        var stationJob = JsonSerializer.Deserialize<StationJobMessage>(message, JsonOptions)
                         ?? throw new InvalidOperationException("Invalid StationJobMessage JSON.");

        await HandleStationJob(stationJob, ct);
    }

    private async Task HandleFanoutStart(Guid parentJobId, CancellationToken ct)
    {
        _logger.LogInformation("Fanout start for ParentJobId={JobId}", parentJobId);


        var http = _httpFactory.CreateClient();
        using var resp = await http.GetAsync("https://data.buienradar.nl/2.0/feed/json", ct);
        resp.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));

        if (!doc.RootElement.TryGetProperty("actual", out var actual) ||
            !actual.TryGetProperty("stationmeasurements", out var arr) ||
            arr.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Buienradar JSON missing actual.stationmeasurements.");


        _logger.LogInformation("arr lenght {lentgh}", arr.GetArrayLength());

        await _repository.UpsertParentStartedAsync(parentJobId, arr.GetArrayLength(), ct);

        foreach (var item in arr.EnumerateArray())
        {

            var stationId = GetString(item, "stationid");
            var stationName = GetString(item, "stationname");
            var imageUrl = GetString(item, "iconurl");

            if (string.IsNullOrWhiteSpace(stationId) ||
                string.IsNullOrWhiteSpace(stationName) ||
                string.IsNullOrWhiteSpace(imageUrl))
                continue;

            var job = new StationJobMessage
            {
                ParentJobId = parentJobId,
                StationId = stationId!,
                StationName = stationName!,
                ImageUrl = imageUrl!
            };

            var payload = JsonSerializer.Serialize(job, JsonOptions);
            await _queueClient.SendMessageAsync(payload, ct);
        }
    }

    private async Task HandleStationJob(StationJobMessage job, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();
        var bytes = await http.GetByteArrayAsync(job.ImageUrl, ct);

        _logger.LogInformation("Fetched station image {Station} bytes={Bytes}",
            job.StationName, bytes.Length);

        await _repository.IncrementCompletedAndMaybeFinishAsync(job.ParentJobId, ct);
    }

    private static string? GetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var p)) return null;
        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number => p.TryGetInt64(out var n) ? n.ToString() : p.ToString(),
            _ => null
        };
    }
}