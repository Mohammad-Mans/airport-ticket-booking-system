using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking> AddAsync(Booking booking);
}