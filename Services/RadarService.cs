using System.Net.Http;
using Company.Function.Domain.Interfaces;

namespace Company.Function.Services;

public sealed class RadarService : IWeatherService
{
    private readonly HttpClient _http;

    public RadarService(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetRawWeatherAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync(
            "https://data.buienradar.nl/2.0/feed/json",
            ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }
}