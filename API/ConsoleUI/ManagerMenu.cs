namespace ATBS.API.ConsoleUI;

public class ManagerMenu : BaseMenu
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
            // case "1":
            //     await FilterBookingsAsync();
            //     return true;
            // case "2":
            //     await UploadFlightsFromCsvAsync();
            //     return true;
            // default:
            //     return false;
        }

        return true;
    }
}