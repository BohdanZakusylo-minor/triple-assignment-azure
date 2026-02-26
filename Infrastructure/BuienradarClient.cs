using System.Text.Json;
using Company.Function.Domain.DTO;
using Company.Function.Infrastrucure.Interfaces;

namespace Company.Function.Infrastructure.Weather;

public sealed class BuienradarClient : IBuienradarClient
{
    private readonly HttpClient _http;

    public BuienradarClient(HttpClient http) => _http = http;

    public async Task<IReadOnlyList<BuienradarStationDto>> GetStationsAsync(int take, CancellationToken ct)
    {
        using var resp = await _http.GetAsync("2.0/feed/json", ct);
        resp.EnsureSuccessStatusCode();

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        if (!doc.RootElement.TryGetProperty("actual", out var actual) ||
            !actual.TryGetProperty("stationmeasurements", out var arr) ||
            arr.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Buienradar JSON does not contain actual.stationmeasurements array.");
        }

        var result = new List<BuienradarStationDto>(take);

        foreach (var item in arr.EnumerateArray())
        {
            if (result.Count >= take) break;

            var stationId = GetString(item, "stationid") ?? GetString(item, "stationId") ?? "";
            var stationName = GetString(item, "stationname") ?? GetString(item, "stationName") ?? "";

            var imageUrl = GetString(item, "iconurl") ?? GetString(item, "iconUrl") ?? "https://picsum.photos/800/600";

            if (string.IsNullOrWhiteSpace(stationId) || string.IsNullOrWhiteSpace(stationName))
                continue;

            result.Add(new BuienradarStationDto
            {
                StationId = stationId,
                StationName = stationName,
                ImageUrl = imageUrl
            });
        }

        return result;
    }

    private static string? GetString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var p)) return null;

        return p.ValueKind switch
        {
            JsonValueKind.String => p.GetString(),
            JsonValueKind.Number => p.TryGetInt64(out var n) ? n.ToString() : p.ToString(),
            _ => null
        };
    }
}
