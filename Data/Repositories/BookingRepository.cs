using System.Globalization;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.Data.Repositories;

public class BookingRepository(string filePath)
    : BaseCsvRepository<Booking>(filePath), IBookingRepository
{
    protected override string Header =>
        "Id,FlightId,PassengerId,Price,Class,Status,CreatedAt";

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        var all = await ReadAllAsync();
        return all.FirstOrDefault(b => b.Id == id);
    }

    public async Task<IReadOnlyList<Booking>> GetByPassengerIdAsync(Guid passengerId)
    {
        var all = await ReadAllAsync();
        return all.Where(b => b.PassengerId == passengerId).ToList();
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        var all = await ReadAllAsync();
        all.Add(booking);
        await WriteAllAsync(all);
        return booking;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, BookingStatus status)
    {
        var all = await ReadAllAsync();
        var idx = all.FindIndex(b => b.Id == id);
        if (idx < 0) return false;

        var current = all[idx];
        all[idx] = new Booking
        {
            Id = current.Id,
            FlightId = current.FlightId,
            PassengerId = current.PassengerId,
            Price = current.Price,
            Class = current.Class,
            Status = status,
            CreatedAt = current.CreatedAt
        };

        await WriteAllAsync(all);
        return true;
    }

    protected override Booking Parse(string line)
    {
        var p = line.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length != 7) throw new FormatException("Invalid CSV line for Booking.");

        return new Booking
        {
            Id = Guid.Parse(p[0]),
            FlightId = Guid.Parse(p[1]),
            PassengerId = Guid.Parse(p[2]),
            Price = decimal.Parse(p[3], NumberStyles.Number, CultureInfo.InvariantCulture),
            Class = Enum.Parse<TravelClass>(p[4], ignoreCase: true),
            Status = Enum.Parse<BookingStatus>(p[5], ignoreCase: true),
            CreatedAt = DateTime.ParseExact(p[6], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
        };
    }

    protected override string Serialize(Booking b)
        => string.Join(',',
            b.Id,
            b.FlightId,
            b.PassengerId,
            b.Price.ToString(CultureInfo.InvariantCulture),
            b.Class.ToString(),
            b.Status.ToString(),
            b.CreatedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}