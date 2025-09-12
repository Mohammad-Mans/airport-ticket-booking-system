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
                (b, f) => new { Booking = b, Flight = f })
            .OrderByDescending(x => x.Booking.CreatedAt)
            .Select(x => new BookingView(
                x.Booking,
                x.Flight.FlightNumber,
                x.Flight.DepartureAirport,
                x.Flight.DepartureCountry,
                x.Flight.ArrivalAirport,
                x.Flight.DestinationCountry,
                x.Flight.DepartureDate,
                x.Flight.ArrivalDate))
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

    public async Task<Booking> ChangeClassAsync(Guid bookingId, TravelClass newClass)
    {
        var booking = await bookingRepo.GetByIdAsync(bookingId)
                      ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status != BookingStatus.Booked)
            throw new InvalidOperationException("Only booked reservations can be modified.");

        if (booking.Class == newClass)
            return booking;

        var passenger = await RequirePassengerAsync(booking.PassengerId);
        var flight = await RequireFlightAsync(booking.FlightId);

        var oldFc = await RequireFlightClassAsync(flight.Id, booking.Class);
        var newFc = await RequireFlightClassAsync(flight.Id, newClass);

        var delta = newFc.Price - oldFc.Price;

        await ReserveNewSeatOrThrow();
        await AdjustBalanceOrThrow();
        await ReleaseOldSeatOrThrow();
        await PersistBookingOrThrow();

        booking.Class = newClass;
        booking.Price = newFc.Price;
        return booking;

        async Task ReserveNewSeatOrThrow()
        {
            var isSeatReserved = await flightClassRepo.TryReserveSeatAsync(flight.Id, newClass);
            if (!isSeatReserved)
                throw new InvalidOperationException("No seats available in the requested class.");
        }

        async Task AdjustBalanceOrThrow()
        {
            if (delta == 0m) return;

            var isBalanceAdjusted = await passengerRepo.TryAdjustBalanceAsync(passenger.Id, -delta);
            if (!isBalanceAdjusted)
            {
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                throw new InvalidOperationException(
                    delta > 0 ? "Insufficient balance for upgrade." : "Refund failed.");
            }
        }

        async Task ReleaseOldSeatOrThrow()
        {
            var isSeatReleased = await flightClassRepo.TryReleaseSeatAsync(flight.Id, booking.Class);
            if (!isSeatReleased)
            {
                if (delta != 0m) await passengerRepo.TryAdjustBalanceAsync(passenger.Id, +delta);
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                throw new InvalidOperationException("Failed to release previous seat.");
            }
        }

        async Task PersistBookingOrThrow()
        {
            var isBookingUpdated = await bookingRepo.UpdateClassAndPriceAsync(booking.Id, newClass, newFc.Price);
            if (!isBookingUpdated)
            {
                await flightClassRepo.TryReserveSeatAsync(flight.Id, booking.Class);
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                if (delta != 0m) await passengerRepo.TryAdjustBalanceAsync(passenger.Id, +delta);
                throw new InvalidOperationException("Failed to update booking.");
            }
        }
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