using Company.Function.Domain.DTO;

namespace Company.Function.Domain.Interfaces;

public interface IBuienradarClient
{
    Task<IReadOnlyList<BuienradarStationDto>> GetStationsAsync(int take, CancellationToken ct);
}