using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.RequestImage;

public class RequestImageAzHttpTrigger
{
    private readonly ILogger<RequestImageAzHttpTrigger> _logger;

    public RequestImageAzHttpTrigger(ILogger<RequestImageAzHttpTrigger> logger)
    {
        _logger = logger;
    }

    [Function("RequestImageAzHttpTrigger")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}