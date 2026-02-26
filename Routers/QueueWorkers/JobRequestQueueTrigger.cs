using System.Net;
using System.Text.Json;
using Company.Function.Domain.Images;
using Company.Function.Infrastrucure.Interfaces;
using Company.Function.Domain.Messages;
using Company.Function.ImageEditor;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.QueueWorkers;

public sealed class JobRequestQueueTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<JobRequestQueueTrigger> _logger;
    private readonly UploadImageHandler _uploadImageHandler;

    private readonly string imagesRequesUrl = "https://picsum.photos/800/600";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public JobRequestQueueTrigger(
        IProcessRecordRepository repository,
        IHttpClientFactory httpFactory,
        ILogger<JobRequestQueueTrigger> logger,
        UploadImageHandler uploadImageHandler)
    {
        _repository = repository;
        _httpFactory = httpFactory;
        _logger = logger;
        _uploadImageHandler = uploadImageHandler;
    }

    [Function("ProcessStationJob")]
    public async Task Run(
        [QueueTrigger("imagequeue", Connection = "AzureWebJobsStorage")] string message,
        CancellationToken ct)
    {
        _logger.LogInformation("imagequeue message received. Length={Len}", message?.Length ?? 0);

        StationJobMessage job;
        try
        {
            job = JsonSerializer.Deserialize<StationJobMessage>(message ?? "{}", JsonOptions)
                  ?? throw new InvalidOperationException("Invalid StationJobMessage JSON.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "imagequeue: invalid message (not StationJobMessage JSON). Raw length={Len}. Check sender writes valid JSON to queue 'imagequeue'.", message?.Length ?? 0);
            throw;
        }

        try
        {
            var http = _httpFactory.CreateClient("images");

            using var response = await http.GetAsync(
                imagesRequesUrl,
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
                (job.StationName, (10, 10), 32, "ffffff"),
                (job.StationId, (10, 44), 24, "000000")
            );

            if (renderedStream.CanSeek) renderedStream.Position = 0;

            var cmd = new UploadImageCommand(renderedStream, "image/png", "rendered.png");
            await _uploadImageHandler.HandleAsync(cmd, job.ParentJobId, ct);

            await _repository.IncrementCompletedAndMaybeFinishAsync(job.ParentJobId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessStationJob failed. ParentJobId={ParentJobId}, StationId={StationId}, Error={ErrorType}: {Message}",
                job.ParentJobId, job.StationId, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}