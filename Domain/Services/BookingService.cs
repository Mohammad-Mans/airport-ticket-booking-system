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

    public async Task<IReadOnlyList<BookingView>> GetByPassengerAsync(Guid passengerId)
    {
        var bookings = await bookingRepo.GetByPassengerIdAsync(passengerId);
        if (bookings.Count == 0) return [];

        var flights = await flightRepo.GetAllAsync();
        var views = bookings
            .Where(b => b.Status == BookingStatus.Booked)
            .Join(
                flights,
                b => b.FlightId,
                f => f.Id,
                (b, f) => new BookingView(
                    b,
                    f.FlightNumber,
                    f.DepartureAirport, f.DepartureCountry,
                    f.ArrivalAirport, f.DestinationCountry,
                    f.DepartureDate, f.ArrivalDate))
            .ToList();

        return views;
    }

    public async Task<bool> CancelAsync(Guid bookingId)
    {
        var booking = await bookingRepo.GetByIdAsync(bookingId);
        if (booking is null) return false;
        if (booking.Status == BookingStatus.Cancelled) return true;

        var passenger = await RequirePassengerAsync(booking.PassengerId);
        await RequireFlightAsync(booking.FlightId);
        await RequireFlightClassAsync(booking.FlightId, booking.Class);

        var refunded = await passengerRepo.TryAdjustBalanceAsync(passenger.Id, +booking.Price);
        if (!refunded)
            throw new InvalidOperationException("Refund failed.");

        var released = await flightClassRepo.TryReleaseSeatAsync(booking.FlightId, booking.Class);
        if (!released)
        {
            await passengerRepo.TryAdjustBalanceAsync(passenger.Id, -booking.Price);
            throw new InvalidOperationException("Failed to restore seat.");
        }

        return await bookingRepo.UpdateStatusAsync(bookingId, BookingStatus.Cancelled);
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