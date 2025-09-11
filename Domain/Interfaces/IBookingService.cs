using ATBS.Domain.Entities;
using ATBS.Domain.Enums;

namespace ATBS.Domain.Interfaces;

public interface IBookingService
{
    Task<Booking> BookAsync(Guid passengerId, Guid flightId, TravelClass travelClass);
}