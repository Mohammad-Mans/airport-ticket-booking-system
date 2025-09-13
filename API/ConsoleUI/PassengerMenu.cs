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
            Console.WriteLine("6. View/Deposit Balance");
            Console.WriteLine("7. Logout");
        }
    }

    protected override async Task<bool> HandleChoiceAsync(string choice)
    {
        if (_currentPassengerId == null)
            return await HandleGuestChoiceAsync(choice);

        return await HandlePassengerChoiceAsync(choice);
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
                await ViewOrDepositBalanceAsync();
                return true;
            case "7":
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
        var first = ConsolePrompts.Required("\nFirst name: ");
        var last = ConsolePrompts.Required("Last name: ");

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
        var first = ConsolePrompts.Required("\nFirst name: ");
        var last = ConsolePrompts.Required("Last name: ");
        var balance = ConsolePrompts.DecimalWithDefault("Initial balance (default 0): ", 0m);

        try
        {
            var passenger = await passengerService.AddPassengerAsync(first, last, balance);
            _currentPassengerId = passenger.Id;

            Console.WriteLine($"\nWelcome, {passenger.FirstName} {passenger.LastName}!");
            Console.WriteLine("You have been registered successfully!");
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

        var idx = ConsolePrompts.Index("\nChoose a flight number to book: ", 1, rows.Count);
        var chosen = rows[idx - 1];

        Console.WriteLine("\nChoose class:");
        for (int i = 0; i < chosen.ClassOptions.Count; i++)
        {
            var o = chosen.ClassOptions[i];
            Console.WriteLine($"{i + 1}. {o.Class} - {o.Price:F2} (Seats:{o.SeatsAvailable})");
        }

        var classIdx = ConsolePrompts.Index("Class option: ", 1, chosen.ClassOptions.Count);
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

            var idx = ConsolePrompts.Index("\nChoose a booking to cancel: ", 1, views.Count);
            var chosen = views[idx - 1];

            if (!ConsolePrompts.Confirm($"Confirm cancel booking #{idx}? (y/n): "))
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

            var idx = ConsolePrompts.Index("\nChoose a booking to modify: ", 1, views.Count);
            var chosen = views[idx - 1];

            Console.WriteLine($"Current class: {chosen.Booking.Class}");
            var newClass =
                ConsolePrompts.ClassRequired("Choose new class (Economy/Business/First): ", chosen.Booking.Class);

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

    private async Task ViewOrDepositBalanceAsync()
    {
        Console.WriteLine();

        try
        {
            var balance = await passengerService.GetBalanceAsync(_currentPassengerId!.Value);
            if (balance is null)
            {
                Console.WriteLine("Passenger not found.");
                WaitForKeyPress();
                return;
            }

            Console.WriteLine($"Your current balance: {balance.Value:F2}");

            if (!ConsolePrompts.Confirm("Would you like to deposit? (y/n): "))
            {
                Console.WriteLine("No deposit made.");
                WaitForKeyPress();
                return;
            }

            var amount = ConsolePrompts.PositiveDecimal("Amount to deposit: ");
            try
            {
                var ok = await passengerService.DepositAsync(_currentPassengerId.Value, amount);
                Console.WriteLine(ok ? "Deposit successful." : "Passenger not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Deposit failed: {ex.Message}");
            }

            var newBal = await passengerService.GetBalanceAsync(_currentPassengerId.Value);
            if (newBal is not null)
                Console.WriteLine($"New balance: {newBal.Value:F2}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve balance: {ex.Message}");
        }

        WaitForKeyPress();
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
            DepartureCountry = ConsolePrompts.Optional("Departure country: "),
            DestinationCountry = ConsolePrompts.Optional("Destination country: "),
            DepartureAirport = ConsolePrompts.Optional("Departure airport: "),
            ArrivalAirport = ConsolePrompts.Optional("Arrival airport: "),
            DepartureDateUtc = ConsolePrompts.DateOnlyOptional("Departure date (dd-MM-yyyy): "),
            Class = ConsolePrompts.ClassOptional("Class (Economy/Business/First): "),
            MaxPrice = ConsolePrompts.DecimalOptional("Max price: "),
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
                $"{classSummary}");
        }
    }
}