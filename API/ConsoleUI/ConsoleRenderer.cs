using ATBS.API.Bookings.Views;
using ATBS.Domain.Interfaces;

namespace ATBS.API.ConsoleUI;

public static class ConsoleRenderer
{
    public static void Flights(IReadOnlyList<FlightOption> rows)
    {
        for (var i = 0; i < rows.Count; i++)
        {
            var o = rows[i];
            Console.WriteLine(
                $"{i + 1}. {o.Flight.FlightNumber} | " +
                $"{FormatRoute(o.Flight.DepartureCountry, o.Flight.DepartureAirport, o.Flight.DestinationCountry, o.Flight.ArrivalAirport)} | " +
                $"{o.Flight.DepartureDate:dd-MM-yyyy HH:mm} | " +
                $"{FormatClassSummary(o.ClassOptions)}");
        }
    }

    public static void MyBookings(IReadOnlyList<BookingView> views)
    {
        for (var i = 0; i < views.Count; i++)
        {
            var v = views[i];
            Console.WriteLine(
                $"{i + 1}. {v.FlightNumber} | " +
                $"{FormatRoute(v.FromCountry, v.FromAirport, v.ToCountry, v.ToAirport)} | " +
                $"{v.Departure:dd-MM-yyyy HH:mm} -> {v.Arrival:dd-MM-yyyy HH:mm} | " +
                $"Class: {v.Booking.Class} | Price: ${v.Booking.Price}");
        }
    }

    public static void BookingSearchResults(IReadOnlyList<BookingSearchView> results)
    {
        Console.WriteLine($"Found {results.Count} booking(s):\n");
        foreach (var v in results)
        {
            var b = v.Booking;
            Console.WriteLine(
                $"[{b.CreatedAt:dd-MM-yyyy HH:mm}] {v.PassengerFirstName} {v.PassengerLastName} | {v.FlightNumber} | " +
                $"{FormatRoute(v.DepartureCountry, v.DepartureAirport, v.DestinationCountry, v.ArrivalAirport)} | " +
                $"{v.DepartureDate:dd-MM-yyyy HH:mm} -> {v.ArrivalDate:dd-MM-yyyy HH:mm}");
            Console.WriteLine($"    Class: {b.Class} | Price: ${b.Price} | Status: {b.Status} | BookingId: {b.Id}");
            Console.WriteLine("------------------");
        }
    }

    private static string FormatClassSummary(IReadOnlyList<FlightClassOption> opts) =>
        string.Join(" | ", opts.Select(co => $"{co.Class}:${co.Price} (Seats:{co.SeatsAvailable})"));

    private static string FormatRoute(string fromCountry, string fromAirport, string toCountry, string toAirport) =>
        $"{fromCountry}:{fromAirport} -> {toCountry}:{toAirport}";
}