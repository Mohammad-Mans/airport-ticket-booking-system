using ATBS.Domain.Entities;

namespace ATBS.API.Bookings.DTOs;

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