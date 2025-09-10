using ATBS.Domain.Entities;

namespace ATBS.Domain.Interfaces;

public interface IFlightClassRepository
{
    Task<IReadOnlyList<FlightClass>> GetAllAsync();
    Task AddOrUpdateRangeAsync(IEnumerable<FlightClass> rows);
}