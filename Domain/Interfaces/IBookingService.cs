using ATBS.API.Bookings.Queries;
using ATBS.API.Bookings.Views;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IBookingService
{
    Task<Booking> BookAsync(Guid passengerId, Guid flightId, TravelClass travelClass);
    Task<IReadOnlyList<BookingView>> GetByPassengerAsync(Guid passengerId);
    Task<bool> CancelAsync(Guid bookingId);
    Task<Booking> ChangeClassAsync(Guid bookingId, TravelClass newClass);
    Task<IReadOnlyList<BookingSearchView>> SearchAsync(BookingSearchQuery query);
}