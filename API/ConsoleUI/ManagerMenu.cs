using ATBS.Domain.Interfaces;
using ATBS.Utils;

namespace ATBS.API.ConsoleUI;

public class ManagerMenu(IFlightService flightService) : BaseMenu
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
            case "2":
                await ImportFlightsAsync();
                return true;
            default:
                return false;
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nImport failed: {ex.Message}");
        }

        WaitForKeyPress();
    }
}