using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Company.Function.Routers.HttpFuncs;

public sealed class RequestImageAZFunction
{
    private readonly ILogger<RequestImageAZFunction> _logger;

    public RequestImageAZFunction(ILogger<RequestImageAZFunction> logger)
    {
        _logger = logger;
    }

    public sealed class Output
    {
        [QueueOutput("fanout-start", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; init; }

        [HttpResult]
        public HttpResponseData? HttpResponse { get; init; }
    }

    [Function("RequestImageGeneration")]
    public async Task<Output> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        var parentJobId = Guid.NewGuid();

        try
        {
            _logger.LogInformation(
                "RequestImageGeneration started. CorrelationId={CorrelationId}, JobId={JobId}",
                correlationId, parentJobId);

            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new { jobId = parentJobId, status = "STARTED" }, ct);

            _logger.LogInformation(
                "RequestImageGeneration accepted. CorrelationId={CorrelationId}, JobId={JobId}",
                correlationId, parentJobId);

            return new Output
            {
                QueueMessage = parentJobId.ToString("N"),
                HttpResponse = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "RequestImageGeneration failed. CorrelationId={CorrelationId}, JobId={JobId}, ErrorType={ErrorType}, Message={Message}",
                correlationId, parentJobId, ex.GetType().Name, ex.Message);

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteAsJsonAsync(new
            {
                error = "Request image generation failed.",
                correlationId,
                jobId = parentJobId,
                errorType = ex.GetType().Name,
                message = ex.Message
            }, ct);
            return new Output { HttpResponse = response };
        }
    }
}