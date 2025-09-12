using System.Globalization;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.Domain.Services;

public class FlightService(IFlightRepository flightRepo, IFlightClassRepository classRepo)
    : IFlightService
{
    public async Task<IReadOnlyList<FlightOption>> SearchAsync(FlightSearchQuery q)
    {
        var flights = await flightRepo.GetAllAsync();
        var classes = await classRepo.GetAllAsync();

        var depCountry = Norm(q.DepartureCountry);
        var dstCountry = Norm(q.DestinationCountry);
        var depAirport = Norm(q.DepartureAirport);
        var arrAirport = Norm(q.ArrivalAirport);
        var depDate = q.DepartureDateUtc;
        var requireSeats = q.OnlyWithSeats;
        var wantedClass = q.Class;
        var maxPrice = q.MaxPrice;

        var results = flights
            .Where(FlightMatchesQuery)
            .GroupJoin(
                classes,
                f => f.Id,
                c => c.FlightId,
                (f, fc) => new { Flight = f, FlightClasses = fc.ToList() }
            )
            .Select(x =>
            {
                var filteredClasses = x.FlightClasses.Where(ClassMatchesQuery)
                    .Select(c => new FlightClassOption(c.Class, c.Price, c.SeatsAvailable))
                    .ToList();

                return new { x.Flight, Filtered = filteredClasses };
            })
            .Where(r => r.Filtered.Count > 0)
            .Select(r => new FlightOption(r.Flight, r.Filtered))
            .ToList();

        return results;

        static string? Norm(string? s) =>
            string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        static bool Eq(string a, string b) =>
            string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        bool FlightMatchesQuery(Flight f) =>
            (depCountry is null || Eq(f.DepartureCountry, depCountry)) &&
            (dstCountry is null || Eq(f.DestinationCountry, dstCountry)) &&
            (depAirport is null || Eq(f.DepartureAirport, depAirport)) &&
            (arrAirport is null || Eq(f.ArrivalAirport, arrAirport)) &&
            (!depDate.HasValue || DateOnly.FromDateTime(f.DepartureDate.ToUniversalTime()) == depDate.Value);

        bool ClassMatchesQuery(FlightClass c) =>
            (!requireSeats || c.SeatsAvailable > 0) &&
            (!wantedClass.HasValue || c.Class == wantedClass.Value) &&
            (!maxPrice.HasValue || c.Price <= maxPrice.Value);
    }

    // Expected manager CSV header:
    // FlightNumber,DepartureAirport,DepartureDate,DepartureCountry,ArrivalAirport,DestinationCountry,ArrivalDate,
    // EconomyPrice,BusinessPrice,FirstPrice,EconomyCapacity,BusinessCapacity,FirstCapacity
    public async Task<FlightImportResult> ImportFromCsvAsync(string csvPath)
    {
        var result = new FlightImportResult();

        var existingFlights = await flightRepo.GetAllAsync();
        var byKey = existingFlights.ToDictionary(ComputeFlightKey, f => f, StringComparer.OrdinalIgnoreCase);

        var newFlights = new List<Flight>();
        var newClasses = new List<FlightClass>();

        using var sr = new StreamReader(csvPath);
        if (!await ReadAndValidateHeaderAsync(sr, result)) return result;

        string? line;
        int row = 1;
        while ((line = await sr.ReadLineAsync()) is not null)
        {
            row++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var lineData = line.Split(',', StringSplitOptions.TrimEntries);
                if (lineData.Length < 13)
                    throw new FormatException("Invalid column count.");

                var flightVals = ParseFlightCore(lineData);
                flightVals = Deduplicate(byKey, flightVals);

                var classVals = ParseClassValues(flightVals.Id, lineData).ToList();
                var hasEconomy = classVals.Any(c => c.Class == TravelClass.Economy);
                if (!hasEconomy)
                    throw new InvalidOperationException(
                        "Economy class must be provided (price and capacity). Other classes are optional.");

                newClasses.AddRange(classVals);
                newFlights.Add(flightVals);
                result.RowsProcessed++;
            }
            catch (Exception ex)
            {
                result.Errors.Add((row, ex.Message));
            }
        }

        await AddOrUpdateFlightsAsync(newFlights, existingFlights);
        await classRepo.AddOrUpdateRangeAsync(newClasses);

        return result;
    }

    private static async Task<bool> ReadAndValidateHeaderAsync(StreamReader sr, FlightImportResult result)
    {
        var header = await sr.ReadLineAsync();
        if (header is null)
        {
            result.Errors.Add((1, "File is empty."));
            return false;
        }

        return true;
    }

    private static Flight ParseFlightCore(string[] p)
    {
        var flightNumber = Require(p[0], "FlightNumber");
        var depAirport = Require(p[1], "DepartureAirport");
        var depCountry = Require(p[2], "DepartureCountry");
        var depUtc = ParseUtc(p[3], "DepartureDate");
        var arrAirport = Require(p[4], "ArrivalAirport");
        var dstCountry = Require(p[5], "DestinationCountry");
        var arrUtc = ParseUtc(p[6], "ArrivalDate");

        if (arrUtc <= depUtc)
            throw new ArgumentException("ArrivalDate must be after DepartureDate.");

        return new Flight
        {
            Id = Guid.NewGuid(),
            FlightNumber = flightNumber,
            DepartureAirport = depAirport,
            DepartureCountry = depCountry,
            ArrivalAirport = arrAirport,
            DestinationCountry = dstCountry,
            DepartureDate = depUtc,
            ArrivalDate = arrUtc
        };
    }

    private static IEnumerable<FlightClass> ParseClassValues(Guid flightId, string[] p)
    {
        var classes = new List<FlightClass>();

        TryAddClass(classes, flightId, TravelClass.Economy, p[7], p[10]);
        TryAddClass(classes, flightId, TravelClass.Business, p[8], p[11]);
        TryAddClass(classes, flightId, TravelClass.First, p[9], p[12]);

        return classes;
    }

    private static void TryAddClass(List<FlightClass> classes, Guid flightId, TravelClass cls, string priceStr,
        string capStr)
    {
        if (string.IsNullOrWhiteSpace(priceStr) || string.IsNullOrWhiteSpace(capStr))
            return;

        var price = ParseDecimalNonNegative(priceStr, $"{cls}Price");
        var cap = ParseIntNonNegative(capStr, $"{cls}Capacity");

        classes.Add(new FlightClass
        {
            FlightId = flightId,
            Class = cls,
            Price = price,
            CapacityTotal = cap,
            SeatsAvailable = cap
        });
    }

    private static Flight Deduplicate(Dictionary<string, Flight> byKey, Flight incoming)
    {
        var key = ComputeFlightKey(incoming);
        if (byKey.TryGetValue(key, out var existing))
        {
            return new Flight
            {
                Id = existing.Id,
                FlightNumber = incoming.FlightNumber,
                DepartureAirport = incoming.DepartureAirport,
                DepartureCountry = incoming.DepartureCountry,
                ArrivalAirport = incoming.ArrivalAirport,
                DestinationCountry = incoming.DestinationCountry,
                DepartureDate = incoming.DepartureDate,
                ArrivalDate = incoming.ArrivalDate
            };
        }

        byKey[key] = incoming;
        return incoming;
    }

    private async Task AddOrUpdateFlightsAsync(List<Flight> flights, IReadOnlyList<Flight> existingFlights)
    {
        foreach (var f in flights)
        {
            var exists = existingFlights.Any(x => x.Id == f.Id);
            if (exists) await flightRepo.UpdateAsync(f);
            else await flightRepo.AddAsync(f);
        }
    }

    private static string Require(string s, string name)
        => string.IsNullOrWhiteSpace(s) ? throw new ArgumentException($"{name} is required.") : s.Trim();

    private static DateTime ParseUtc(string s, string name)
    {
        var styles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal;
        if (DateTime.TryParseExact(s, "O", CultureInfo.InvariantCulture, styles, out var dt)) return dt;
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, styles, out dt)) return dt;
        throw new FormatException($"{name} is not a valid UTC/ISO date.");
    }

    private static decimal ParseDecimalNonNegative(string s, string name)
    {
        if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
            throw new FormatException($"{name} is not a valid number.");
        if (d < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
        return d;
    }

    private static int ParseIntNonNegative(string s, string name)
    {
        if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
            throw new FormatException($"{name} is not a valid integer.");
        if (i < 0) throw new ArgumentOutOfRangeException(name, $"{name} cannot be negative.");
        return i;
    }

    private static string ComputeFlightKey(Flight f)
        => $"{f.FlightNumber}|{f.DepartureAirport}|{f.ArrivalAirport}|{f.DepartureDate:O}";
}