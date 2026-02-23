using Azure.Data.Tables;
using Azure.Storage.Queues;
using Company.Function.Domain.Interfaces;
using Company.Function.Infrastructure.TableStorage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Function.Infrastructure;

public static class TableStorageServiceCollectionExtensions
{
    public static IServiceCollection AddTableStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["TableStorageConnection"]
            ?? configuration["AzureWebJobsStorage"]
            ?? throw new InvalidOperationException("Table storage connection string not found. Set TableStorageConnection or AzureWebJobsStorage.");

        var tableName = configuration["TableStorage:TableName"] ?? "statustable";
        var queueName = configuration["TableStorage:QueueName"] ?? "imagequeue";

        services.AddSingleton(sp => new TableClient(connectionString, tableName));
        services.AddSingleton(sp => new QueueClient(connectionString, queueName));
        services.AddSingleton<IProcessRecordRepository, ProcessRecordRepository>();

        return services;
    }
}
