using System.Text.Json;
using Company.Function.Domain.Interfaces;
using Company.Function.Domain.Messages;

namespace Company.Function.Application;

public sealed class StationFanOutService
{
    private readonly IBuienradarClient _client;
    private readonly IJobQueue _queue;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public StationFanOutService(IBuienradarClient client, IJobQueue queue)
    {
        _client = client;
        _queue = queue;
    }

    /// <summary>
    /// Fetches stations from Buienradar and enqueues a <see cref="StationJobMessage"/> per station for the given parent job.
    /// </summary>
    /// <returns>Number of messages enqueued.</returns>
    public async Task<int> FanOutAsync(Guid parentJobId, int count, CancellationToken ct)
    {
        var stations = await _client.GetStationsAsync(count, ct);

        foreach (var s in stations)
        {
            var msg = new StationJobMessage
            {
                ParentJobId = parentJobId,
                StationId = s.StationId,
                StationName = s.StationName,
                ImageUrl = s.ImageUrl
            };

            var payload = JsonSerializer.Serialize(msg, JsonOptions);
            await _queue.EnqueueAsync(payload, ct);
        }

        return stations.Count;
    }
}