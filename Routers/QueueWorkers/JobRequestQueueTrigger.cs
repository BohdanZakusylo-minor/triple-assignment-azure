using System.Net;
using System.Text.Json;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Messages;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Compnay.Function.ImageEditor;

namespace Company.Function.Routers.QueueWorkers;

public sealed class JobRequestQueueTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<JobRequestQueueTrigger> _logger;
    private readonly IBlobStorage _blobStorage;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobRequestQueueTrigger(
        IProcessRecordRepository repository,
        IHttpClientFactory httpFactory,
        ILogger<JobRequestQueueTrigger> logger,
        IBlobStorage blobStorage)
    {
        _repository = repository;
        _httpFactory = httpFactory;
        _logger = logger;
        _blobStorage = blobStorage;
    }

    [Function("ProcessStationJob")]
    public async Task Run(
        [QueueTrigger("imagequeue", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken ct)
    {

        var job = JsonSerializer.Deserialize<StationJobMessage>(message, JsonOptions)
                  ?? throw new InvalidOperationException("Invalid StationJobMessage JSON.");

        var http = _httpFactory.CreateClient("images");

        using var response = await http.GetAsync(
            job.ImageUrl,
            HttpCompletionOption.ResponseHeadersRead,
            ct);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            (int)response.StatusCode == 429)
        {
            throw new HttpRequestException($"Remote service throttled: {(int)response.StatusCode}");
        }

        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Not an image. Content-Type={contentType ?? "null"}");

        await using var imgStream = await response.Content.ReadAsStreamAsync(ct);

        var renderedStream = ImageHelper.AddTextToImage(
            imgStream,
            ("What do you call a developer who doesn't comment code?", (10, 10), 32, "ffffff"),
            ("A developer", (10, 44), 24, "000000")
        );

        if (renderedStream.CanSeek) renderedStream.Position = 0;

        var blobUrl = await _blobStorage.UploadAsync(
            renderedStream,
            contentType: "image/png",
            fileName: "rendered.png",
            ct: ct,
            parentId: job.ParentJobId

        );

        await _repository.IncrementCompletedAndMaybeFinishAsync(job.ParentJobId, ct);
    }
}