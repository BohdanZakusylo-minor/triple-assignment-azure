using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.Function;

public class RequestImagesFunction
{
    private readonly ILogger<RequestImagesFunction> _logger;

    public RequestImagesFunction(ILogger<RequestImagesFunction> logger)
    {
        _logger = logger;
    }

    [Function("RequestImagesFunction")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}