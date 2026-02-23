using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Company.Function.Domain.Enums;

namespace Company.Function.Routers.HttpFuncs;

public sealed class RequestImageAZFunction
{
    [Function("RequestImageGeneration")]
    [QueueOutput("imagequeue", Connection = "AzureWebJobsStorage")]
    public IActionResult RequestImageGeneration(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        // absolute minimal message (plain text)
        var jobId = Guid.NewGuid();
        var status = JobStatusEnum.STARTED.ToString();

        // this string will be enqueued by the runtime
        var msg = $"{jobId}|{status}";

        return new OkObjectResult(new { jobId, status, enqueued = msg });
    }
}