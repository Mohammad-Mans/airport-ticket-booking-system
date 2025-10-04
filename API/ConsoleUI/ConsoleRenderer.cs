using ATBS.API.Bookings.DTOs;
using ATBS.API.Flights.DTOs;

namespace ATBS.API.ConsoleUI;

public static class ConsoleRenderer
{
    public static void Flights(IReadOnlyList<FlightSearchView> rows)
    {
        var index = 0;
        foreach (var flightView in rows)
        {
            Console.WriteLine(
                $"{index + 1}. {flightView.Flight.FlightNumber} | " +
                $"{FormatRoute(flightView.Flight.DepartureCountry, flightView.Flight.DepartureAirport, flightView.Flight.DestinationCountry, flightView.Flight.ArrivalAirport)} | " +
                $"{flightView.Flight.DepartureDate:dd-MM-yyyy HH:mm} | " +
                $"{FormatClassSummary(flightView.ClassOptions)}");
            index++;
        }
    }

    public static void MyBookings(IReadOnlyList<BookingView> views)
    {
        var index = 0;
        foreach (var bookingView in views)
        {
            Console.WriteLine(
                $"{index + 1}. {bookingView.FlightNumber} | " +
                $"{FormatRoute(bookingView.FromCountry, bookingView.FromAirport, bookingView.ToCountry, bookingView.ToAirport)} | " +
                $"{bookingView.Departure:dd-MM-yyyy HH:mm} -> {bookingView.Arrival:dd-MM-yyyy HH:mm} | " +
                $"Class: {bookingView.Booking.Class} | Price: ${bookingView.Booking.Price}");
            index++;
        }
    }

    public static void BookingSearchResults(IReadOnlyList<BookingSearchView> results)
    {
        Console.WriteLine($"Found {results.Count} booking(s):\n");
        foreach (var searchResult in results)
        {
            var booking = searchResult.Booking;
            Console.WriteLine(
                $"[{booking.CreatedAt:dd-MM-yyyy HH:mm}] {searchResult.PassengerFirstName} {searchResult.PassengerLastName} | {searchResult.FlightNumber} | " +
                $"{FormatRoute(searchResult.DepartureCountry, searchResult.DepartureAirport, searchResult.DestinationCountry, searchResult.ArrivalAirport)} | " +
                $"{searchResult.DepartureDate:dd-MM-yyyy HH:mm} -> {searchResult.ArrivalDate:dd-MM-yyyy HH:mm}");
            Console.WriteLine(
                $"    Class: {booking.Class} | Price: ${booking.Price} | Status: {booking.Status} | BookingId: {booking.Id}");
            Console.WriteLine("------------------");
        }
    }

    private static string FormatClassSummary(IReadOnlyList<FlightClassOption> opts) =>
        string.Join(" | ", opts.Select(co => $"{co.Class}:${co.Price} (Seats:{co.SeatsAvailable})"));

    private static string FormatRoute(string fromCountry, string fromAirport, string toCountry, string toAirport) =>
        $"{fromCountry}:{fromAirport} -> {toCountry}:{toAirport}";
}