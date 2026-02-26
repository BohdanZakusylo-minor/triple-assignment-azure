namespace Company.Function.Infrastrucure.Interfaces;

public interface IWeatherService
{
    Task<string> GetRawWeatherAsync(CancellationToken ct = default);
}