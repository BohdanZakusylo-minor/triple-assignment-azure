using System.Text.Json;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Messages;
using Company.Function.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.QueueWorkers;

public sealed class BuienradarFanoutTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ImageQueue _imageQueue;
    private readonly ILogger<BuienradarFanoutTrigger> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const string ImageApiURL = "https://picsum.photos/800/600";

    public BuienradarFanoutTrigger(
        IProcessRecordRepository repository,
        IHttpClientFactory httpFactory,
        ImageQueue imageQueue,
        ILogger<BuienradarFanoutTrigger> logger)
    {
        _repository = repository;
        _httpFactory = httpFactory;
        _imageQueue = imageQueue;
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

        var http = _httpFactory.CreateClient("buienradar");
        using var resp = await http.GetAsync("2.0/feed/json", ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("actual", out var actual) ||
            !actual.TryGetProperty("stationmeasurements", out var arr) ||
            arr.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Buienradar JSON missing actual.stationmeasurements array.");
        }

        var queued = 0;

        foreach (var item in arr.EnumerateArray())
        {
            if (queued >= 50) break;

            var stationId = GetString(item, "stationid");
            var stationName = GetString(item, "stationname");

            if (string.IsNullOrWhiteSpace(stationId) || string.IsNullOrWhiteSpace(stationName))
                continue;

            var job = new StationJobMessage
            {
                ParentJobId = parentJobId,
                StationId = stationId!,
                StationName = stationName!,
                ImageUrl = ImageApiURL
            };

            var payload = JsonSerializer.Serialize(job, JsonOptions);
            try
            {
                await _imageQueue.Client.SendMessageAsync(payload, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "imagequeue send failed. Queue=imagequeue, ParentJobId={ParentJobId}, StationId={StationId}, MessageLength={Len}. Check AzureWebJobsStorage and queue 'imagequeue' exists.",
                    parentJobId, stationId, payload?.Length ?? 0);
                throw;
            }
            queued++;
        }

        await _repository.UpsertParentStartedAsync(parentJobId, queued, ct);

        _logger.LogInformation("Fanout done: ParentJobId={ParentJobId}, queued={Queued}", parentJobId, queued);
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