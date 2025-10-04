using ATBS.Domain.Entities;

namespace ATBS.Domain.Interfaces;

public interface IFlightRepository
{
    Task<IReadOnlyList<Flight>> GetAllAsync();
    Task<Flight?> GetByIdAsync(Guid id);
    Task<Flight> AddAsync(Flight flight);
    Task<Flight> UpdateAsync(Flight flight);
}