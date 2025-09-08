using ATBS.API.ConsoleUI;

namespace ATBS.API.ConsoleUI;

public class MainMenu : BaseMenu
{
    protected override void DisplayMenu()
    {
        DisplayHeader("Airport Ticket Booking System");
        Console.WriteLine("1. Passenger");
        Console.WriteLine("2. Manager");
        Console.WriteLine("0. Exit");
    }

    protected override async Task<bool> HandleChoiceAsync(string choice)
    {
        switch (choice)
        {
            case "1":
                var passengerMenu = new PassengerMenu();
                await passengerMenu.RunAsync();
                return true;
            case "2":
                var managerMenu = new ManagerMenu();
                await managerMenu.RunAsync();
                return true;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                WaitForKeyPress();
                return true;
        }
    }
}