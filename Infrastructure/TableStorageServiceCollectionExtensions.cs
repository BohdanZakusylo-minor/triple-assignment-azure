using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Company.Function.Domain.Interfaces;
using Company.Function.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Company.Function.Infrastructure;

public sealed class FanoutStartQueue
{
    public QueueClient Client { get; }
    public FanoutStartQueue(QueueClient client) => Client = client;
}

public sealed class ImageQueue
{
    public QueueClient Client { get; }
    public ImageQueue(QueueClient client) => Client = client;
}

public static class TableStorageServiceCollectionExtensions
{
    public static IServiceCollection AddTableStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var cs = configuration["TableStorageConnection"]
                 ?? configuration["AzureWebJobsStorage"]
                 ?? throw new InvalidOperationException("Set TableStorageConnection or AzureWebJobsStorage.");

        var tableName = configuration["TableStorage:TableName"] ?? "statustable";

        services.AddSingleton(_ =>
        {
            var client = new TableClient(cs, tableName);
            client.CreateIfNotExists();
            return client;
        });
        services.AddSingleton<IProcessRecordRepository, ProcessRecordRepository>();

        services.AddSingleton(_ =>
        {
            var q = new QueueClient(cs, "fanout-start", new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
            q.CreateIfNotExists();
            return new FanoutStartQueue(q);
        });

        services.AddSingleton(_ =>
        {
            var q = new QueueClient(cs, "imagequeue", new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64
            });
            q.CreateIfNotExists();
            return new ImageQueue(q);
        });

        services.AddSingleton(_ => new AzureBlobStorageOptions
        {
            ConnectionString = configuration["BlobStorageConnection"]
                               ?? configuration["AzureWebJobsStorage"]
                               ?? throw new InvalidOperationException("Set BlobStorageConnection or AzureWebJobsStorage."),
            ContainerName = configuration["BlobStorage:ContainerName"] ?? "blob-for-tripple"
        });

        services.AddSingleton<IBlobStorage, AzureBlobStorage>();

        services.AddHttpClient("buienradar", c =>
        {
            c.BaseAddress = new Uri("https://data.buienradar.nl/");
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient("images", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}