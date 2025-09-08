using ATBS.Domain.Entities;
using ATBS.Domain.Interfaces;

namespace ATBS.Domain.Services;

public class PassengerService(IPassengerRepository passengerRepository) : IPassengerService
{
    public async Task<Passenger> AddPassengerAsync(string firstName, string lastName, decimal initialBalance = 0m)
    {
        var (normalizedFirst, normalizedLast) = NormalizeNames(firstName, lastName);

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

    public Task<Passenger?> GetByIdAsync(Guid id)
        => passengerRepository.GetByIdAsync(id);

    public async Task<Passenger?> GetByNameAsync(string firstName, string lastName)
    {
        var (normalizedFirst, normalizedLast) = NormalizeNames(firstName, lastName);
        return await passengerRepository.FindByNameAsync(normalizedFirst, normalizedLast);
    }

    public Task<IReadOnlyList<Passenger>> GetAllPassengersAsync()
        => passengerRepository.GetAllAsync();

    public async Task<Passenger> UpdatePassengerAsync(Passenger passenger)
    {
        if (passenger == null)
            throw new ArgumentNullException(nameof(passenger));

        return await passengerRepository.UpdateAsync(passenger);
    }

    private static (string normalizedFirst, string normalizedLast) NormalizeNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        return (firstName.Trim(), lastName.Trim());
    }
}