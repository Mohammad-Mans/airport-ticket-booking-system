using System.Globalization;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.Domain.Services;

public class FlightService(IFlightRepository flightRepo, IFlightClassRepository classRepo)
    : IFlightService
{
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

                var classVals = ParseClassValues(lineData);
                
                AddClassRows(newClasses, flightVals.Id, classVals);
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

    private static (decimal ecoPrice, decimal busPrice, decimal firstPrice,
        int ecoCap, int busCap, int firstCap) ParseClassValues(string[] p)
    {
        var ecoPrice = ParseDecimalNonNegative(p[7], "EconomyPrice");
        var busPrice = ParseDecimalNonNegative(p[8], "BusinessPrice");
        var firstPrice = ParseDecimalNonNegative(p[9], "FirstPrice");

        var ecoCap = ParseIntNonNegative(p[10], "EconomyCapacity");
        var busCap = ParseIntNonNegative(p[11], "BusinessCapacity");
        var firstCap = ParseIntNonNegative(p[12], "FirstCapacity");

        return (ecoPrice, busPrice, firstPrice, ecoCap, busCap, firstCap);
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

    private static void AddClassRows(
        List<FlightClass> allClasses,
        Guid flightId,
        (decimal ecoPrice, decimal busPrice, decimal firstPrice, int ecoCap, int busCap, int firstCap) v)
    {
        allClasses.Add(new FlightClass
        {
            FlightId = flightId,
            Class = TravelClass.Economy,
            Price = v.ecoPrice,
            CapacityTotal = v.ecoCap,
            SeatsAvailable = v.ecoCap
        });
        allClasses.Add(new FlightClass
        {
            FlightId = flightId,
            Class = TravelClass.Business,
            Price = v.busPrice,
            CapacityTotal = v.busCap,
            SeatsAvailable = v.busCap
        });
        allClasses.Add(new FlightClass
        {
            FlightId = flightId,
            Class = TravelClass.First,
            Price = v.firstPrice,
            CapacityTotal = v.firstCap,
            SeatsAvailable = v.firstCap
        });
    }

    private async Task AddOrUpdateFlightsAsync(
        List<Flight> flights, IReadOnlyList<Flight> existingFlights)
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