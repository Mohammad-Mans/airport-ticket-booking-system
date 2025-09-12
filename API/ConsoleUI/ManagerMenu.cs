using System.Globalization;
using ATBS.API.Validation;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;
using ATBS.Utils;

namespace ATBS.API.ConsoleUI;

public class ManagerMenu(IFlightService flightService, IBookingService bookingService) : BaseMenu
{
    protected override void DisplayMenu()
    {
        DisplayHeader("Manager Menu");
        Console.WriteLine("1. Filter Bookings");
        Console.WriteLine("2. Upload Flights from CSV");
        Console.WriteLine("0. Back to Main Menu");
    }

    protected override async Task<bool> HandleChoiceAsync(string choice)
    {
        switch (choice)
        {
            case "1":
                await FilterBookingsAsync();
                return true;
            case "2":
                await ImportFlightsAsync();
                return true;
            default:
                return false;
        }
    }

    private async Task FilterBookingsAsync()
    {
        Console.WriteLine("\nEnter booking filters (leave blank to skip):");

        string? firstName = Prompt("Passenger first name: ");
        string? lastName = Prompt("Passenger last name: ");
        string? flightNumber = Prompt("Flight number: ");
        string? depCountry = Prompt("Departure country: ");
        string? dstCountry = Prompt("Destination country: ");
        string? depAirport = Prompt("Departure airport: ");
        string? arrAirport = Prompt("Arrival airport: ");
        DateOnly? depDate = PromptDateOnlyOptional("Departure date (UTC, dd-MM-yyyy): ");
        TravelClass? travelClass = PromptClassOptional("Class (Economy/Business/First): ");
        decimal? maxPrice = PromptDecimalOptional("Max price: ");

        var q = new BookingSearchQuery
        {
            PassengerFirstName = firstName,
            PassengerLastName = lastName,
            FlightNumber = flightNumber,
            DepartureCountry = depCountry,
            DestinationCountry = dstCountry,
            DepartureAirport = depAirport,
            ArrivalAirport = arrAirport,
            DepartureDateUtc = depDate,
            Class = travelClass,
            MaxPrice = maxPrice
        };

        var results = await bookingService.SearchAsync(q);

        Console.WriteLine();
        if (results.Count == 0)
        {
            Console.WriteLine("No bookings found.");
            WaitForKeyPress();
            return;
        }

        Console.WriteLine($"Found {results.Count} booking(s):\n");
        foreach (var v in results)
        {
            var b = v.Booking;
            Console.WriteLine(
                $"[{b.CreatedAt:yyyy-MM-dd HH:mm}] {v.PassengerFirstName} {v.PassengerLastName} | {v.FlightNumber} | " +
                $"{v.DepartureCountry}:{v.DepartureAirport} -> {v.DestinationCountry}:{v.ArrivalAirport} | {v.DepartureDate:yyyy-MM-dd HH:mm} -> {v.ArrivalDate:yyyy-MM-dd HH:mm}" +
                $"\n    Class: {b.Class} | Price: {b.Price:C} | Status: {b.Status} | BookingId: {b.Id}");
            Console.WriteLine("------------------");
        }

        WaitForKeyPress();

        static string? Prompt(string label)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        static DateOnly? PromptDateOnlyOptional(string label)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (DateOnly.TryParseExact(s.Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var d))
                return d;

            Console.WriteLine("  Invalid date. Ignored.");
            return null;
        }

        static TravelClass? PromptClassOptional(string label)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return null;

            s = s.Trim().ToLowerInvariant();
            return s switch
            {
                "economy" => TravelClass.Economy,
                "business" => TravelClass.Business,
                "first" => TravelClass.First,
                _ => null
            };
        }

        static decimal? PromptDecimalOptional(string label)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(s)) return null;
            return decimal.TryParse(s.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        }
    }


    private async Task ImportFlightsAsync()
    {
        var path = FilePaths.ManagerImportFlightsPath;
        try
        {
            var result = await flightService.ImportFromCsvAsync(path);

            Console.WriteLine($"\nProcessed: {result.RowsProcessed}");
            if (result.Errors.Count > 0)
            {
                Console.WriteLine("\nErrors:");
                foreach (var (row, err) in result.Errors)
                    Console.WriteLine($"Row {row}: {err}");

                DocConstraintsPrinter.PrintFor(
                    title: "Flight Import Fields",
                    typeof(Flight),
                    typeof(FlightClass)
                );
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nImport failed: {ex.Message}");
        }

        WaitForKeyPress();
    }
}