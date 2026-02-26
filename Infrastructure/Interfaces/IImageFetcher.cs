namespace Company.Function.Infrastrucure.Interfaces;

public interface IImageFetcher
{
    Task<byte[]> FetchAsync(string imageUrl, CancellationToken ct);
}