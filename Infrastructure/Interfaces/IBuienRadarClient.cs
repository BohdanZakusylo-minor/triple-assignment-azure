using Company.Function.Domain.DTO;

namespace Company.Function.Infrastrucure.Interfaces;

public interface IBuienradarClient
{
    Task<IReadOnlyList<BuienradarStationDto>> GetStationsAsync(int take, CancellationToken ct);
}