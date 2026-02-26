using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Company.Function.Infrastrucure.Interfaces;

public interface IBlobStorage
{
    Task<BlobUploadResult> UploadAsync(
        Stream content,
        string contentType,
        string fileName,
        Guid parentId,
        CancellationToken ct = default);
    Task<IReadOnlyList<BlobUploadResult>> ListByParentIdAsync(Guid parentId, CancellationToken ct = default);
    Task<IReadOnlyList<BlobUploadResult>> ListByParentIdWithSasAsync(
        Guid parentId,
        TimeSpan sasValidity,
        CancellationToken ct = default);
}

public sealed record BlobUploadResult(string BlobName, string Url);
