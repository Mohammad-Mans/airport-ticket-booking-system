namespace ATBS.API.ConsoleUI;

public class PassengerMenu : BaseMenu
{
    private Guid? _currentPassengerId;

    protected override void DisplayMenu()
    {
        DisplayHeader("Passenger Menu");

        if (_currentPassengerId == null)
        {
            Console.WriteLine("1. Login/Register");
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
            Console.WriteLine("0. Back to Main Menu");
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
            // case "1":
            //     await LoginOrRegisterAsync();
            //     return true;
            // default:
            //     return false;
        }

        return true;
    }

    private async Task<bool> HandlePassengerChoiceAsync(string choice)
    {
        switch (choice)
        {
            // case "1":
            //     await SearchFlightsAsync();
            //     return true;
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
            // case "6":
            //     _currentPassengerId = null;
            //     Console.WriteLine("Logged out successfully.");
            //     WaitForKeyPress();
            //     return true;
            // default:
            //     return false;
        }

        return true;
    }
}