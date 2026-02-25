using System.Threading;
using System.Threading.Tasks;
using Company.Function.Domain.Interfaces;

namespace Company.Function.Domain.Images;

public sealed class UploadImageHandler
{
    private readonly IBlobStorage _blobStorage;

    public UploadImageHandler(IBlobStorage blobStorage)
    {
        _blobStorage = blobStorage;
    }

    public Task<BlobUploadResult> HandleAsync(UploadImageCommand cmd, Guid parentId, CancellationToken ct = default)
        => _blobStorage.UploadAsync(cmd.Content, cmd.ContentType, cmd.FileName, parentId, ct);
}
