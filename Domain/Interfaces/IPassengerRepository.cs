using ATBS.Domain.Entities;

namespace ATBS.Domain.Interfaces;

public interface IPassengerRepository
{
    Task<Passenger?> GetByIdAsync(Guid id);
    Task<Passenger?> FindByNameAsync(string firstName, string lastName);
    Task<IReadOnlyList<Passenger>> GetAllAsync();
    Task<Passenger> AddAsync(Passenger passenger);
    Task<Passenger> UpdateAsync(Passenger passenger);
    Task<bool> TryAdjustBalanceAsync(Guid passengerId, decimal delta);
}