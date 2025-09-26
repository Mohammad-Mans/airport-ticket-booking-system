using ATBS.Domain.Entities;

namespace ATBS.API.Bookings.Views;

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