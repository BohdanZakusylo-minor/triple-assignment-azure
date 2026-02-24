using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Company.Function.Routers.HttpFuncs;

public sealed class RequestImageAZFunction
{
    public sealed class Output
    {
        [QueueOutput("imagequeue", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; init; }

        [HttpResult]
        public HttpResponseData? HttpResponse { get; init; }
    }

    [Function("RequestImageGeneration")]
    public async Task<Output> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        CancellationToken ct)
    {
        var parentJobId = Guid.NewGuid();

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { jobId = parentJobId, status = "QUEUED" }, ct);

        return new Output
        {
            QueueMessage = parentJobId.ToString("N"),
            HttpResponse = response
        };
    }
}