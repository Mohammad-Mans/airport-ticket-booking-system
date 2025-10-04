using ATBS.Domain.Entities;
using ATBS.Domain.Interfaces;
using System.Globalization;

namespace ATBS.Data.Repositories;

public class PassengerRepository(string filePath) : BaseCsvRepository<Passenger>(filePath), IPassengerRepository
{

    public async Task<Passenger?> GetByIdAsync(Guid id)
    {
        var all = await ReadAllAsync();
        return all.FirstOrDefault(p => p.Id == id);
    }

    public async Task<Passenger?> FindByNameAsync(string firstName, string lastName)
    {
        var all = await ReadAllAsync();
        return all.FirstOrDefault(p =>
            string.Equals(p.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.LastName, lastName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<Passenger>> GetAllAsync()
        => await ReadAllAsync();

    public async Task<Passenger> AddAsync(Passenger passenger)
    {
        var all = await ReadAllAsync();
        all.Add(passenger);
        await WriteAllAsync(all);
        return passenger;
    }

    public async Task<bool> TryAdjustBalanceAsync(Guid passengerId, decimal amount)
    {
        var all = await ReadAllAsync();
        var idx = all.FindIndex(p => p.Id == passengerId);
        if (idx < 0) return false;

        var p = all[idx];
        var newBalance = p.Balance + amount;
        if (newBalance < 0) return false;

        p.Balance = newBalance;
        all[idx] = p;
        await WriteAllAsync(all);
        return true;
    }

    protected override Passenger Parse(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 4) throw new FormatException("Invalid CSV line for Passenger.");

        return new Passenger
        {
            Id = Guid.Parse(parts[0]),
            FirstName = parts[1],
            LastName = parts[2],
            Balance = decimal.Parse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture)
        };
    }

    protected override string Serialize(Passenger p)
    {
        return string.Join(',',
            p.Id.ToString(),
            p.FirstName,
            p.LastName,
            p.Balance.ToString(CultureInfo.InvariantCulture));
    }
}