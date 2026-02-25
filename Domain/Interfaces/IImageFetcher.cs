namespace Company.Function.Application.Abstractions;

public interface IImageFetcher
{
    Task<byte[]> FetchAsync(string imageUrl, CancellationToken ct);
}