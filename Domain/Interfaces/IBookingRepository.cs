using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IBookingRepository
{
    Task<IReadOnlyList<Booking>> GetByPassengerIdAsync(Guid passengerId);
    Task<Booking> AddAsync(Booking booking);
}