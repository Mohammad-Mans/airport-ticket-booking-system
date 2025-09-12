using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetAllAsync();
    Task<Booking?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Booking>> GetByPassengerIdAsync(Guid passengerId);
    Task<Booking> AddAsync(Booking booking);
    Task<bool> UpdateStatusAsync(Guid id, BookingStatus status);
    Task<bool> UpdateClassAndPriceAsync(Guid bookingId, TravelClass newClass, decimal newPrice);
}