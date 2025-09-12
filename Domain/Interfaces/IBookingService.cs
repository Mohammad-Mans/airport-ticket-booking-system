using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public sealed record BookingView(
    Booking Booking,
    string FlightNumber,
    string FromAirport,
    string FromCountry,
    string ToAirport,
    string ToCountry,
    DateTime Departure,
    DateTime Arrival
);

public sealed class BookingSearchQuery
{
    public string? PassengerFirstName { get; set; }
    public string? PassengerLastName { get; set; }
    public string? FlightNumber { get; set; }
    public string? DepartureCountry { get; set; }
    public string? DestinationCountry { get; set; }
    public string? DepartureAirport { get; set; }
    public string? ArrivalAirport { get; set; }
    public DateOnly? DepartureDateUtc { get; set; }
    public TravelClass? Class { get; set; }
    public decimal? MaxPrice { get; set; }
}

public sealed record BookingSearchView(
    Booking Booking,
    string FlightNumber,
    string DepartureAirport,
    string DepartureCountry,
    string ArrivalAirport,
    string DestinationCountry,
    DateTime DepartureDate,
    DateTime ArrivalDate,
    string PassengerFirstName,
    string PassengerLastName
);

public interface IBookingService
{
    Task<Booking> BookAsync(Guid passengerId, Guid flightId, TravelClass travelClass);
    Task<IReadOnlyList<BookingView>> GetByPassengerAsync(Guid passengerId);
    Task<bool> CancelAsync(Guid bookingId);
    Task<Booking> ChangeClassAsync(Guid bookingId, TravelClass newClass);
    Task<IReadOnlyList<BookingSearchView>> SearchAsync(BookingSearchQuery query);
}