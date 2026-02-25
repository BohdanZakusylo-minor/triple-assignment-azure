namespace Company.Function.Domain.Interfaces;

public interface IWeatherService
{
    Task<string> GetRawWeatherAsync(CancellationToken ct = default);
}