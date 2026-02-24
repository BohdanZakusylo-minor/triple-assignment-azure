using System.Net;
using Company.Function.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.HttpFuncs;

public sealed class JobStatusHttpTrigger
{
    private readonly IProcessRecordRepository _repository;
    private readonly IBlobStorage _blobStorage;
    private readonly ILogger<JobStatusHttpTrigger> _logger;

    public JobStatusHttpTrigger(
        IProcessRecordRepository repository,
        IBlobStorage blobStorage,
        ILogger<JobStatusHttpTrigger> logger)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    [Function("GetJobStatus")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
        CancellationToken ct)
    {
        var jobId = req.Query["jobId"];
        if (string.IsNullOrWhiteSpace(jobId) || !Guid.TryParse(jobId, out var id))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteAsJsonAsync(new { error = "Missing or invalid jobId." }, ct);
            return badRequest;
        }

        var status = await _repository.GetByJobIdAsync(id, ct);
        if (status is null)
        {
            _logger.LogInformation("Job not found: {JobId}", id);
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteAsJsonAsync(new { error = "Job not found.", jobId = id }, ct);
            return notFound;
        }

        var blobs = await _blobStorage.ListByParentIdAsync(id, ct);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            jobId = status.JobId,
            status = status.Status,
            completed = status.Completed,
            total = status.Total,
            outputs = blobs.Select(b => new { name = b.BlobName, url = b.Url }).ToList()
        }, ct);
        return response;
    }
}
