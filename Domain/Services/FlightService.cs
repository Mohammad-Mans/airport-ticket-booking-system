using ATBS.API.Flights.Queries;
using ATBS.API.Flights.Views;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;
using ATBS.Domain.Utils;
using ATBS.Utils;

namespace ATBS.Domain.Services;

public class FlightService(IFlightRepository flightRepo, IFlightClassRepository classRepo)
    : IFlightService
{
    public async Task<IReadOnlyList<FlightSearchView>> SearchAsync(FlightSearchQuery q)
    {
        var flights = await flightRepo.GetAllAsync();
        var classes = await classRepo.GetAllAsync();

        var departureCountry = StringUtils.Normalize(q.DepartureCountry);
        var destinationCountry = StringUtils.Normalize(q.DestinationCountry);
        var departureAirport = StringUtils.Normalize(q.DepartureAirport);
        var arrivalAirport = StringUtils.Normalize(q.ArrivalAirport);
        var departureDate = q.DepartureDateUtc;
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
            .Select(r => new FlightSearchView(r.Flight, r.Filtered))
            .ToList();

        return results;

        bool FlightMatchesQuery(Flight f) =>
            (departureCountry is null || StringUtils.EqualsIgnoreCase(f.DepartureCountry, departureCountry)) &&
            (destinationCountry is null || StringUtils.EqualsIgnoreCase(f.DestinationCountry, destinationCountry)) &&
            (departureAirport is null || StringUtils.EqualsIgnoreCase(f.DepartureAirport, departureAirport)) &&
            (arrivalAirport is null || StringUtils.EqualsIgnoreCase(f.ArrivalAirport, arrivalAirport)) &&
            (!departureDate.HasValue ||
             DateOnly.FromDateTime(f.DepartureDate.ToUniversalTime()) == departureDate.Value);

        bool ClassMatchesQuery(FlightClass c) =>
            (!requireSeats || c.SeatsAvailable > 0) &&
            (!wantedClass.HasValue || c.Class == wantedClass.Value) &&
            (!maxPrice.HasValue || c.Price <= maxPrice.Value);
    }

    private static readonly string[] ExpectedCsvHeaders =
    [
        "FlightNumber", "DepartureAirport", "DepartureCountry", "DepartureDate",
        "ArrivalAirport", "DestinationCountry", "ArrivalDate",
        "EconomyPrice", "BusinessPrice", "FirstPrice",
        "EconomyCapacity", "BusinessCapacity", "FirstCapacity"
    ];

    public async Task<FlightImportResult> ImportFromCsvAsync(string csvPath)
    {
        var result = new FlightImportResult();

        var existingFlights = await flightRepo.GetAllAsync();
        var byKey = existingFlights.ToDictionary(FlightUtils.ComputeFlightKey, f => f,
            StringComparer.OrdinalIgnoreCase);

        var newFlights = new List<Flight>();
        var newClasses = new List<FlightClass>();

        using var sr = new StreamReader(csvPath);
        var expectedColumnsCount = ExpectedCsvHeaders.Length;
        if (!await ReadAndValidateHeaderAsync(sr, result)) return result;

        string? line;
        var row = 1;
        while ((line = await sr.ReadLineAsync()) is not null)
        {
            row++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var lineData = line.Split(',', StringSplitOptions.TrimEntries);
                if (lineData.Length < expectedColumnsCount)
                    throw new FormatException(
                        $"Invalid column count. Expected {expectedColumnsCount} columns but found {lineData.Length}.");

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

        var headerColumns = header.Split(',', StringSplitOptions.TrimEntries);
        var expectedColumnsCount = ExpectedCsvHeaders.Length;
        if (headerColumns.Length != expectedColumnsCount)
        {
            result.Errors.Add((1,
                $"Invalid header column count. Expected {expectedColumnsCount} columns but found {headerColumns.Length}."));
            return false;
        }

        for (var i = 0; i < expectedColumnsCount; i++)
        {
            if (!StringUtils.EqualsIgnoreCase(headerColumns[i], ExpectedCsvHeaders[i]))
            {
                result.Errors.Add((1,
                    $"Invalid header at column {i + 1}. Expected '{ExpectedCsvHeaders[i]}' but found '{headerColumns[i]}'."));
                return false;
            }
        }

        return true;
    }

    private static Flight ParseFlightCore(string[] p)
    {
        var flightNumber = FlightUtils.RequireNonEmpty(p[0], "FlightNumber");
        var departureAirport = FlightUtils.RequireNonEmpty(p[1], "DepartureAirport");
        var departureCountry = FlightUtils.RequireNonEmpty(p[2], "DepartureCountry");
        var departureUtc = ParsingUtils.ParseUtc(p[3], "DepartureDate");
        var arrivalAirport = FlightUtils.RequireNonEmpty(p[4], "ArrivalAirport");
        var destinationCountry = FlightUtils.RequireNonEmpty(p[5], "DestinationCountry");
        var arrivalUtc = ParsingUtils.ParseUtc(p[6], "ArrivalDate");

        if (arrivalUtc <= departureUtc)
            throw new ArgumentException("ArrivalDate must be after DepartureDate.");

        return new Flight
        {
            Id = Guid.NewGuid(),
            FlightNumber = flightNumber,
            DepartureAirport = departureAirport,
            DepartureCountry = departureCountry,
            ArrivalAirport = arrivalAirport,
            DestinationCountry = destinationCountry,
            DepartureDate = departureUtc,
            ArrivalDate = arrivalUtc
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

        var price = ParsingUtils.ParseDecimalNonNegative(priceStr, $"{cls}Price");
        var cap = ParsingUtils.ParseIntNonNegative(capStr, $"{cls}Capacity");

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
        var key = FlightUtils.ComputeFlightKey(incoming);
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
}