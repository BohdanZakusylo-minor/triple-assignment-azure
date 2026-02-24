namespace Company.Function.Infrastructure;

public sealed class AzureBlobStorageOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string ContainerName { get; init; } = "images";
}