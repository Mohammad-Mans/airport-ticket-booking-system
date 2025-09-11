using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IFlightClassRepository
{
    Task<IReadOnlyList<FlightClass>> GetAllAsync();
    Task<FlightClass?> GetByFlightAndClassAsync(Guid flightId, TravelClass travelClass);
    Task AddOrUpdateRangeAsync(IEnumerable<FlightClass> rows);
    Task<bool> TryReserveSeatAsync(Guid flightId, TravelClass travelClass);
    Task<bool> TryReleaseSeatAsync(Guid flightId, TravelClass travelClass);
}