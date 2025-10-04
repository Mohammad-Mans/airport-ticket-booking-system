using ATBS.Domain.Entities;
using ATBS.Domain.Interfaces;
using ATBS.Domain.Utils;

namespace ATBS.Domain.Services;

public class PassengerService(IPassengerRepository passengerRepository) : IPassengerService
{
    public async Task<Passenger> AddPassengerAsync(string firstName, string lastName, decimal initialBalance = 0m)
    {
        var (normalizedFirst, normalizedLast) = PassengerUtils.NormalizeNames(firstName, lastName);

        var existing = await passengerRepository.FindByNameAsync(normalizedFirst, normalizedLast);
        if (existing != null)
            throw new InvalidOperationException("A passenger with this name already exists.");

        var newPassenger = new Passenger
        {
            FirstName = normalizedFirst,
            LastName = normalizedLast,
            Balance = initialBalance
        };

        return await passengerRepository.AddAsync(newPassenger);
    }

    public async Task<Passenger?> GetByNameAsync(string firstName, string lastName)
    {
        var (normalizedFirst, normalizedLast) = PassengerUtils.NormalizeNames(firstName, lastName);
        return await passengerRepository.FindByNameAsync(normalizedFirst, normalizedLast);
    }

    public async Task<decimal?> GetBalanceAsync(Guid passengerId)
    {
        var p = await passengerRepository.GetByIdAsync(passengerId);
        return p?.Balance;
    }

    public async Task<bool> DepositAsync(Guid passengerId, decimal amount)
    {
        if (amount <= 0m) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
        return await passengerRepository.TryAdjustBalanceAsync(passengerId, amount);
    }

}