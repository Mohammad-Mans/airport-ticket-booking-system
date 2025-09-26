namespace ATBS.Utils;

public static class FilePaths
{
    public static readonly string PassengerFilePath = GetFilePath("Passengers");
    public static readonly string FlightFilePath = GetFilePath("Flights");
    public static readonly string FlightClassFilePath = GetFilePath("FlightClasses");
    public static readonly string BookingFilePath = GetFilePath("Bookings");
    public static readonly string ManagerImportFlightsPath = GetImportPath("FlightsFromManager");

    private static string GetFilePath(string fileName)
    {
        var parentPath = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.ToString();

        if (parentPath == null)
            throw new DirectoryNotFoundException("Could not find project root directory");

        return Path.Combine(parentPath, "Seed", $"{fileName}.csv");
    }

    private static string GetImportPath(string fileName)
    {
        var parentPath = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.ToString();
        if (parentPath == null)
            throw new DirectoryNotFoundException("Could not find project root directory");

        return Path.Combine(parentPath, "Seed", "Imports", $"{fileName}.csv");
    }
}