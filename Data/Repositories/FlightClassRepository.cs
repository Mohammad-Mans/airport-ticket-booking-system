using System.Globalization;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.Data.Repositories;

public class FlightClassRepository(string filePath)
    : BaseCsvRepository<FlightClass>(filePath), IFlightClassRepository
{
    protected override string Header =>
        "FlightId,Class,Price,CapacityTotal,SeatsAvailable";

    public async Task<IReadOnlyList<FlightClass>> GetAllAsync() => await ReadAllAsync();

    public async Task AddOrUpdateRangeAsync(IEnumerable<FlightClass> rows)
    {
        var all = await ReadAllAsync();

        foreach (var row in rows)
        {
            var idx = all.FindIndex(r => r.FlightId == row.FlightId && r.Class == row.Class);
            if (idx >= 0) all[idx] = row;
            else all.Add(row);
        }

        await WriteAllAsync(all);
    }

    protected override FlightClass Parse(string line)
    {
        var p = line.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length != 5) throw new FormatException("Invalid CSV line for FlightClass.");

        if (!Enum.TryParse<TravelClass>(p[1], ignoreCase: true, out var cls))
            throw new FormatException($"Invalid TravelClass: {p[1]}");

        return new FlightClass
        {
            FlightId = Guid.Parse(p[0]),
            Class = cls,
            Price = decimal.Parse(p[2], NumberStyles.Number, CultureInfo.InvariantCulture),
            CapacityTotal = int.Parse(p[3]),
            SeatsAvailable = int.Parse(p[4]),
        };
    }

    protected override string Serialize(FlightClass r)
        => string.Join(',',
            r.FlightId,
            r.Class,
            r.Price.ToString(CultureInfo.InvariantCulture),
            r.CapacityTotal.ToString(),
            r.SeatsAvailable.ToString());
}