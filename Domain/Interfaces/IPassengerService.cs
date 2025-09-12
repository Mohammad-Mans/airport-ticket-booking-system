using ATBS.Domain.Entities;

namespace ATBS.Domain.Interfaces;

public interface IPassengerService
{
    Task<Passenger> AddPassengerAsync(string firstName, string lastName, decimal initialBalance = 0m);
    Task<Passenger?> GetByNameAsync(string firstName, string lastName);
    Task<decimal?> GetBalanceAsync(Guid passengerId);
    Task<bool> DepositAsync(Guid passengerId, decimal amount);
}