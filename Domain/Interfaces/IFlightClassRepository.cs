using ATBS.Domain.Entities;

namespace ATBS.Domain.Interfaces;

public interface IFlightClassRepository
{
    Task AddOrUpdateRangeAsync(IEnumerable<FlightClass> rows);
}