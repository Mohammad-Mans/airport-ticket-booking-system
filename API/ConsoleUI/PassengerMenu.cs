using System.Globalization;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.API.ConsoleUI;

public class PassengerMenu(
    IPassengerService passengerService,
    IFlightService flightService,
    IBookingService bookingService) : BaseMenu
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
            case "2":
                await BookFlightAsync();
                return true;
            case "3":
                await ViewMyBookingsAsync();
                return true;
            case "4":
                await CancelBookingAsync();
                return true;
            case "5":
                await ModifyBookingAsync();
                return true;
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
        var rows = await SearchFlightsWithPromptAsync();
        if (rows is null) return;
        WaitForKeyPress();
    }

    private async Task BookFlightAsync()
    {
        Console.WriteLine();
        var rows = await SearchFlightsWithPromptAsync();
        if (rows is null) return;

        var idx = PromptIndex("\nChoose a flight number to book: ", 1, rows.Count);
        var chosen = rows[idx - 1];

        Console.WriteLine("\nChoose class:");
        for (int i = 0; i < chosen.ClassOptions.Count; i++)
        {
            var o = chosen.ClassOptions[i];
            Console.WriteLine($"{i + 1}. {o.Class} - {o.Price:F2} (Seats:{o.SeatsAvailable})");
        }

        var classIdx = PromptIndex("Class option: ", 1, chosen.ClassOptions.Count);
        var chosenClass = chosen.ClassOptions[classIdx - 1];

        try
        {
            var booking =
                await bookingService.BookAsync(_currentPassengerId!.Value, chosen.Flight.Id, chosenClass.Class);
            Console.WriteLine($"\nBooked! #{booking.Id}");
            Console.WriteLine(
                $"Flight: {chosen.Flight.FlightNumber} | Class: {booking.Class} | Price: {booking.Price:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nBooking failed: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private async Task ViewMyBookingsAsync()
    {
        try
        {
            var views = await bookingService.GetByPassengerAsync(_currentPassengerId!.Value);

            Console.WriteLine();
            if (views.Count == 0)
            {
                Console.WriteLine("You have no bookings.");
            }
            else
            {
                RenderBookings(views);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to load bookings: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private async Task CancelBookingAsync()
    {
        try
        {
            var views = await bookingService.GetByPassengerAsync(_currentPassengerId!.Value);

            Console.WriteLine();
            if (views.Count == 0)
            {
                Console.WriteLine("You have no active bookings to cancel.");
                WaitForKeyPress();
                return;
            }

            RenderBookings(views);

            var idx = PromptIndex("\nChoose a booking to cancel: ", 1, views.Count);
            var chosen = views[idx - 1];

            Console.Write($"Confirm cancel booking #{idx}? (y/n): ");
            var choice = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();
            if (choice is not "y" and not "yes")
            {
                Console.WriteLine("Cancellation aborted.");
                WaitForKeyPress();
                return;
            }

            var isCancelled = await bookingService.CancelAsync(chosen.Booking.Id);
            Console.WriteLine(isCancelled ? "Booking cancelled." : "Booking not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCancel failed: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private async Task ModifyBookingAsync()
    {
        try
        {
            var views = await bookingService.GetByPassengerAsync(_currentPassengerId!.Value);

            Console.WriteLine();
            if (views.Count == 0)
            {
                Console.WriteLine("You have no active bookings to modify.");
                WaitForKeyPress();
                return;
            }

            RenderBookings(views);

            var idx = PromptIndex("\nChoose a booking to modify: ", 1, views.Count);
            var chosen = views[idx - 1];

            Console.WriteLine($"Current class: {chosen.Booking.Class}");
            var newClass = PromptClassRequired("Choose new class (Economy/Business/First): ", chosen.Booking.Class);

            try
            {
                var updated = await bookingService.ChangeClassAsync(chosen.Booking.Id, newClass);
                Console.WriteLine("\nBooking updated.");
                Console.WriteLine(
                    $"{chosen.FlightNumber} | " +
                    $"{chosen.FromCountry}:{chosen.FromAirport} -> {chosen.ToCountry}:{chosen.ToAirport} | " +
                    $"Class: {updated.Class} | Price: {updated.Price:F2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nModify failed: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nFailed to load bookings: {ex.Message}");
        }

        WaitForKeyPress();
    }

    private static TravelClass PromptClassRequired(string label, TravelClass currentClass)
    {
        while (true)
        {
            Console.Write(label);
            var s = (Console.ReadLine() ?? "").Trim().ToLowerInvariant();

            TravelClass? parsed = s switch
            {
                "e" or "economy" => TravelClass.Economy,
                "b" or "business" => TravelClass.Business,
                "f" or "first" => TravelClass.First,
                _ => null
            };

            if (parsed is null)
            {
                Console.WriteLine("Unknown class. Please enter Economy/Business/First.");
                continue;
            }

            if (parsed.Value == currentClass)
            {
                Console.WriteLine("New class must be different from current class.");
                continue;
            }

            return parsed.Value;
        }
    }

    private static void RenderBookings(IReadOnlyList<BookingView> views)
    {
        for (int i = 0; i < views.Count; i++)
        {
            var v = views[i];
            Console.WriteLine(
                $"{i + 1}. {v.FlightNumber} | " +
                $"{v.FromCountry}:{v.FromAirport} -> {v.ToCountry}:{v.ToAirport} | " +
                $"{v.Departure:dd-MM-yyyy HH:mm} -> {v.Arrival:dd-MM-yyyy HH:mm} | " +
                $"Class: {v.Booking.Class} | Price: {v.Booking.Price:F2}");
        }
    }

    private async Task<IReadOnlyList<FlightOption>?> SearchFlightsWithPromptAsync()
    {
        Console.WriteLine("\nEnter search filters (leave any field blank to skip):");
        var q = BuildSearchQuery();

        var rows = await flightService.SearchAsync(q);

        Console.WriteLine();
        if (rows.Count == 0)
        {
            Console.WriteLine("No flights match your criteria.");
            return null;
        }

        RenderFlights(rows);
        return rows;
    }

    private static FlightSearchQuery BuildSearchQuery()
        => new()
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

    private static void RenderFlights(IReadOnlyList<FlightOption> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var o = rows[i];
            var classSummary = string.Join(" | ",
                o.ClassOptions.Select(co => $"{co.Class}:{co.Price:F2}$ (Seats:{co.SeatsAvailable})"));

            Console.WriteLine(
                $"{i + 1}. {o.Flight.FlightNumber} | " +
                $"{o.Flight.DepartureCountry}:{o.Flight.DepartureAirport} -> {o.Flight.DestinationCountry}:{o.Flight.ArrivalAirport} | " +
                $"{o.Flight.DepartureDate:dd-MM-yyyy HH:mm} | " +
                $"{classSummary} | Seats Total: {o.TotalSeats}");
        }
    }

    private static int PromptIndex(string label, int minInclusive, int maxInclusive)
    {
        while (true)
        {
            Console.Write(label);
            var s = Console.ReadLine();
            if (int.TryParse(s, out var n) && n >= minInclusive && n <= maxInclusive) return n;
            Console.WriteLine($"Enter a number between {minInclusive} and {maxInclusive}.");
        }
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