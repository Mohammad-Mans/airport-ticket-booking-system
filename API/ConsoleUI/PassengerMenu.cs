using System.Globalization;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.API.ConsoleUI;

public class PassengerMenu(IPassengerService passengerService, IFlightService flightService) : BaseMenu
{
    private Guid? _currentPassengerId;

    protected override void DisplayMenu()
    {
        DisplayHeader("Passenger Menu");

        if (_currentPassengerId == null)
        {
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Register");
            Console.WriteLine("0. Back to Main Menu");
        }
        else
        {
            Console.WriteLine("1. Search Flights");
            Console.WriteLine("2. Book a Flight");
            Console.WriteLine("3. View My Bookings");
            Console.WriteLine("4. Cancel Booking");
            Console.WriteLine("5. Modify Booking");
            Console.WriteLine("6. Logout");
        }
    }

    protected override async Task<bool> HandleChoiceAsync(string choice)
    {
        if (_currentPassengerId == null)
        {
            return await HandleGuestChoiceAsync(choice);
        }
        else
        {
            return await HandlePassengerChoiceAsync(choice);
        }
    }

    private async Task<bool> HandleGuestChoiceAsync(string choice)
    {
        switch (choice)
        {
            case "1":
                await LoginAsync();
                return true;
            case "2":
                await RegisterAsync();
                return true;
            default:
                return false;
        }
    }

    private async Task<bool> HandlePassengerChoiceAsync(string choice)
    {
        switch (choice)
        {
            case "1":
                await SearchFlightsAsync();
                return true;
            // case "2":
            //     await BookFlightAsync();
            //     return true;
            // case "3":
            //     await ViewMyBookingsAsync();
            //     return true;
            // case "4":
            //     await CancelBookingAsync();
            //     return true;
            // case "5":
            //     await ModifyBookingAsync();
            //     return true;
            case "6":
                _currentPassengerId = null;
                Console.WriteLine("Logged out successfully.");
                WaitForKeyPress();
                return true;
            default:
                return false;
        }
    }

    private async Task LoginAsync()
    {
        var first = PromptNonEmpty("\nFirst name: ");
        var last = PromptNonEmpty("Last name: ");

        try
        {
            var passenger = await passengerService.GetByNameAsync(first, last);

            if (passenger == null)
            {
                Console.WriteLine("\nUser doesn't exist!");
            }
            else
            {
                _currentPassengerId = passenger.Id;
                Console.WriteLine($"\nWelcome back, {passenger.FirstName} {passenger.LastName}!");
                Console.WriteLine($"Your Balance: {passenger.Balance:F2}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nLogin failed: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private async Task RegisterAsync()
    {
        var first = PromptNonEmpty("\nFirst name: ");
        var last = PromptNonEmpty("Last name: ");
        var balance = PromptDecimal("Initial balance (default 0): ", defaultValue: 0m);

        try
        {
            var passenger = await passengerService.AddPassengerAsync(first, last, balance);
            _currentPassengerId = passenger.Id;

            Console.WriteLine($"\nWelcome, {passenger.FirstName} {passenger.LastName}!");
            Console.WriteLine($"You have been registered successfully!");
            Console.WriteLine($"Your Balance: {passenger.Balance:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nRegistration failed: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private async Task SearchFlightsAsync()
    {
        Console.WriteLine();

        Console.WriteLine("\nEnter search filters (leave any field blank to skip):");
        var q = new FlightSearchQuery
        {
            DepartureCountry = PromptOptional("Departure country: "),
            DestinationCountry = PromptOptional("Destination country: "),
            DepartureAirport = PromptOptional("Departure airport: "),
            ArrivalAirport = PromptOptional("Arrival airport: "),
            DepartureDateUtc = PromptDateOnlyOptional("Departure date (dd-MM-yyyy): "),
            Class = PromptClassOptional("Class (Economy/Business/First): "),
            MaxPrice = PromptDecimalOptional("Max price: "),
            OnlyWithSeats = true
        };

        var options = await flightService.SearchAsync(q);

        Console.WriteLine();
        if (options.Count == 0)
        {
            Console.WriteLine("No flights match your criteria.");
            WaitForKeyPress();
            return;
        }

        int i = 1;
        foreach (var o in options)
        {
            Console.WriteLine(
                $"{i++}. {o.Flight.FlightNumber} | " +
                $"{o.Flight.DepartureCountry}:{o.Flight.DepartureAirport} -> {o.Flight.DestinationCountry}:{o.Flight.ArrivalAirport} | " +
                $"{o.Flight.DepartureDate:dd-MM-yyyy HH:mm} | " +
                $"{o.Class} | {o.Price:F2} | Seats: {o.SeatsAvailable}");
        }

        WaitForKeyPress();
    }

    private static string? PromptOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static DateOnly? PromptDateOnlyOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateOnly.TryParseExact(s.Trim(), "d-M-yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;

        Console.WriteLine("Invalid date format (expected dd-MM-yyyy). Ignoring.");
        return null;
    }

    private static TravelClass? PromptClassOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;

        s = s.Trim().ToLowerInvariant();
        return s switch
        {
            "e" or "economy" => TravelClass.Economy,
            "b" or "business" => TravelClass.Business,
            "f" or "first" => TravelClass.First,
            _ => PrintAndReturnNull()
        };

        static TravelClass? PrintAndReturnNull()
        {
            Console.WriteLine("Unknown class. Ignoring.");
            return null;
        }
    }

    private static decimal? PromptDecimalOptional(string label)
    {
        Console.Write(label);
        var s = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(s)) return null;

        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) ||
            decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
            return d;

        Console.WriteLine("Invalid number. Ignoring.");
        return null;
    }

    private static string PromptNonEmpty(string label)
    {
        while (true)
        {
            Console.Write(label);
            var input = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(input)) return input;
            Console.WriteLine("This field is required. Please try again.");
        }
    }

    // Prompts the user for a number and parses it as decimal.
    // Tries CurrentCulture first, then InvariantCulture (so "23,99" and "23.99" both work).
    // Returns defaultValue if input is empty or invalid.
    private static decimal PromptDecimal(string label, decimal defaultValue)
    {
        Console.Write(label);
        var enteredValue = Console.ReadLine();

        if (decimal.TryParse(enteredValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var d) ||
            decimal.TryParse(enteredValue, NumberStyles.Number, CultureInfo.InvariantCulture, out d))
            return d;

        return defaultValue;
    }
}