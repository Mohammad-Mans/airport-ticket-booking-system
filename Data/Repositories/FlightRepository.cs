using System.Globalization;
using ATBS.Domain.Entities;
using ATBS.Domain.Interfaces;

namespace ATBS.Data.Repositories;

public class FlightRepository(string filePath)
    : BaseCsvRepository<Flight>(filePath), IFlightRepository
{
    protected override string Header =>
        "Id,FlightNumber,DepartureAirport,DepartureCountry,DepartureDate,ArrivalAirport,DestinationCountry,ArrivalDate";

    public async Task<IReadOnlyList<Flight>> GetAllAsync()
        => await ReadAllAsync();

    public async Task<Flight?> GetByIdAsync(Guid id)
    {
        var all = await ReadAllAsync();
        return all.FirstOrDefault(f => f.Id == id);
    }

    public async Task<Flight> AddAsync(Flight flight)
    {
        var all = await ReadAllAsync();
        all.Add(flight);
        await WriteAllAsync(all);
        return flight;
    }

    public async Task<Flight> UpdateAsync(Flight flight)
    {
        var all = await ReadAllAsync();
        var idx = all.FindIndex(f => f.Id == flight.Id);
        if (idx < 0) throw new InvalidOperationException("Flight not found.");
        all[idx] = flight;
        await WriteAllAsync(all);
        return flight;
    }

    protected override Flight Parse(string line)
    {
        var p = line.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length != 8) throw new FormatException("Invalid CSV line for Flight.");

        return new Flight
        {
            Id = Guid.Parse(p[0]),
            FlightNumber = p[1],
            DepartureAirport = p[2],
            DepartureCountry = p[3],
            DepartureDate = DateTime.ParseExact(p[4], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
            ArrivalAirport = p[5],
            DestinationCountry = p[6],
            ArrivalDate = DateTime.ParseExact(p[7], "O", CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
        };
    }

    protected override string Serialize(Flight f)
        => string.Join(',',
            f.Id,
            f.FlightNumber,
            f.DepartureAirport,
            f.DepartureCountry,
            f.DepartureDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            f.ArrivalAirport,
            f.DestinationCountry,
            f.ArrivalDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
}