using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;

namespace ATBS.Domain.Services;

public class BookingService(
    IBookingRepository bookingRepo,
    IPassengerRepository passengerRepo,
    IFlightRepository flightRepo,
    IFlightClassRepository flightClassRepo)
    : IBookingService
{
    public async Task<Booking> BookAsync(Guid passengerId, Guid flightId, TravelClass travelClass)
    {
        var passenger = await RequirePassengerAsync(passengerId);
        await RequireFlightAsync(flightId);
        var fc = await RequireFlightClassAsync(flightId, travelClass);

        if (passenger.Balance < fc.Price)
            throw new InvalidOperationException("Insufficient balance.");

        var reserved = await flightClassRepo.TryReserveSeatAsync(flightId, travelClass);
        if (!reserved)
            throw new InvalidOperationException("No seats available for the selected class.");

        var charged = await passengerRepo.TryAdjustBalanceAsync(passengerId, -fc.Price);
        if (!charged)
        {
            await flightClassRepo.TryReleaseSeatAsync(flightId, travelClass);
            throw new InvalidOperationException("Insufficient balance.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PassengerId = passengerId,
            FlightId = flightId,
            Class = travelClass,
            Price = fc.Price,
            Status = BookingStatus.Booked,
        };

        return await bookingRepo.AddAsync(booking);
    }

    private async Task<Passenger> RequirePassengerAsync(Guid id) =>
        await passengerRepo.GetByIdAsync(id)
        ?? throw new InvalidOperationException("Passenger not found.");

    private async Task<Flight> RequireFlightAsync(Guid id) =>
        await flightRepo.GetByIdAsync(id)
        ?? throw new InvalidOperationException("Flight not found.");

    private async Task<FlightClass> RequireFlightClassAsync(Guid flightId, TravelClass travelClass) =>
        await flightClassRepo.GetByFlightAndClassAsync(flightId, travelClass)
        ?? throw new InvalidOperationException("Requested class not available for this flight.");
}