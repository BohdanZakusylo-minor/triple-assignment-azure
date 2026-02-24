using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Company.Function.Domain.Interfaces;
using Company.Function.Infrastructure;

namespace Company.Function.Repository;

public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobStorage(AzureBlobStorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("Blob connection string is missing.", nameof(options.ConnectionString));

        if (string.IsNullOrWhiteSpace(options.ContainerName))
            throw new ArgumentException("Container name is missing.", nameof(options.ContainerName));

        var service = new BlobServiceClient(options.ConnectionString);
        _container = service.GetBlobContainerClient(options.ContainerName);

        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<BlobUploadResult> UploadAsync(
        Stream content,
        string contentType,
        string fileName,
        Guid parentId,
        CancellationToken ct = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));
        if (string.IsNullOrWhiteSpace(contentType)) throw new ArgumentException("Missing content type.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Missing file name.", nameof(fileName));

        var safeExt = Path.GetExtension(fileName);
        var blobName = $"{parentId}/{Guid.NewGuid():N}{safeExt}".ToLowerInvariant();

        var blob = _container.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType,
            CacheControl = "public, max-age=31536000"
        };

        content.Position = 0; // important if stream was read earlier

        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = headers },
            ct);

        return new BlobUploadResult(blobName, blob.Uri.ToString());
    }

    public async Task<IReadOnlyList<BlobUploadResult>> ListByParentIdAsync(Guid parentId, CancellationToken ct = default)
    {
        // Must match upload path: UploadAsync uses $"{parentId}/..." (default Guid = hyphenated)
        var prefix = $"{parentId}/".ToLowerInvariant();
        var results = new List<BlobUploadResult>();
        await foreach (var item in _container.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, ct))
        {
            var blobClient = _container.GetBlobClient(item.Name);
            results.Add(new BlobUploadResult(item.Name, blobClient.Uri.ToString()));
        }
        return results;
    }
}