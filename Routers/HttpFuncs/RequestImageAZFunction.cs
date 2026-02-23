using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Company.Function.Domain.Enums;

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
    public Output Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        var jobId = Guid.NewGuid();
        var status = JobStatusEnum.STARTED.ToString();

        var msg = $"{jobId}|{status}";

        var response = req.CreateResponse(HttpStatusCode.OK);

        return new Output
        {
            QueueMessage = msg,
            HttpResponse = response
        };
    }
}