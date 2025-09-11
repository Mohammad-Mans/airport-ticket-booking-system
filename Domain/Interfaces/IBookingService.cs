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
    DateTime Arrival);

public interface IBookingService
{
    Task<Booking> BookAsync(Guid passengerId, Guid flightId, TravelClass travelClass);
    Task<IReadOnlyList<BookingView>> GetByPassengerAsync(Guid passengerId);
}