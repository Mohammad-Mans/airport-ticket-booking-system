using ATBS.API.Bookings.Queries;
using ATBS.API.Bookings.Views;
using ATBS.Domain.Entities;
using ATBS.Domain.Enums;
using ATBS.Domain.Interfaces;
using ATBS.Utils;

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

        var oldFlightClass = await RequireFlightClassAsync(flight.Id, booking.Class);
        var newFlightClass = await RequireFlightClassAsync(flight.Id, newClass);

        var priceDifference = newFlightClass.Price - oldFlightClass.Price;

        await ReserveNewSeatOrThrow();
        await AdjustBalanceOrThrow();
        await ReleaseOldSeatOrThrow();
        await PersistBookingOrThrow();

        booking.Class = newClass;
        booking.Price = newFlightClass.Price;
        return booking;

        async Task ReserveNewSeatOrThrow()
        {
            var isSeatReserved = await flightClassRepo.TryReserveSeatAsync(flight.Id, newClass);
            if (!isSeatReserved)
                throw new InvalidOperationException("No seats available in the requested class.");
        }

        async Task AdjustBalanceOrThrow()
        {
            if (priceDifference == 0m) return;

            var isBalanceAdjusted = await passengerRepo.TryAdjustBalanceAsync(passenger.Id, -priceDifference);
            if (!isBalanceAdjusted)
            {
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                throw new InvalidOperationException(
                    priceDifference > 0 ? "Insufficient balance for upgrade." : "Refund failed.");
            }
        }

        async Task ReleaseOldSeatOrThrow()
        {
            var isSeatReleased = await flightClassRepo.TryReleaseSeatAsync(flight.Id, booking.Class);
            if (!isSeatReleased)
            {
                if (priceDifference != 0m) await passengerRepo.TryAdjustBalanceAsync(passenger.Id, +priceDifference);
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                throw new InvalidOperationException("Failed to release previous seat.");
            }
        }

        async Task PersistBookingOrThrow()
        {
            var isBookingUpdated =
                await bookingRepo.UpdateClassAndPriceAsync(booking.Id, newClass, newFlightClass.Price);
            if (!isBookingUpdated)
            {
                await flightClassRepo.TryReserveSeatAsync(flight.Id, booking.Class);
                await flightClassRepo.TryReleaseSeatAsync(flight.Id, newClass);
                if (priceDifference != 0m) await passengerRepo.TryAdjustBalanceAsync(passenger.Id, +priceDifference);
                throw new InvalidOperationException("Failed to update booking.");
            }
        }
    }

    public async Task<IReadOnlyList<BookingSearchView>> SearchAsync(BookingSearchQuery q)
    {
        var bookingsTask = bookingRepo.GetAllAsync();
        var flightsTask = flightRepo.GetAllAsync();
        var passengersTask = passengerRepo.GetAllAsync();
        await Task.WhenAll(bookingsTask, flightsTask, passengersTask);

        var bookings = await bookingsTask;
        var flights = await flightsTask;
        var passengers = await passengersTask;

        var firstName = StringUtils.Normalize(q.PassengerFirstName);
        var lastName = StringUtils.Normalize(q.PassengerLastName);
        var flightNumber = StringUtils.Normalize(q.FlightNumber);
        var departureCountry = StringUtils.Normalize(q.DepartureCountry);
        var destinationCountry = StringUtils.Normalize(q.DestinationCountry);
        var departureAirport = StringUtils.Normalize(q.DepartureAirport);
        var arrivalAirport = StringUtils.Normalize(q.ArrivalAirport);
        var departureDate = q.DepartureDateUtc;
        var wantedClass = q.Class;
        var maxPrice = q.MaxPrice;

        return QueryBookings(
            bookings,
            flights,
            passengers,
            firstName,
            lastName,
            flightNumber,
            departureCountry,
            destinationCountry,
            departureAirport,
            arrivalAirport,
            departureDate,
            wantedClass,
            maxPrice
        ).ToList();
    }

    private static IEnumerable<BookingSearchView> QueryBookings(
        IEnumerable<Booking> bookings,
        IEnumerable<Flight> flights,
        IEnumerable<Passenger> passengers,
        string? firstName, string? lastName, string? flightNumber,
        string? departureCountry, string? destinationCountry,
        string? departureAirport, string? arrivalAirport,
        DateOnly? departureDate, TravelClass? wantedClass, decimal? maxPrice)
    {
        bool Matches(Booking b, Flight f, Passenger p)
        {
            var depUtc = f.DepartureDate.ToUniversalTime();
            return
                (firstName is null || StringUtils.EqualsIgnoreCase(p.FirstName, firstName)) &&
                (lastName is null || StringUtils.EqualsIgnoreCase(p.LastName, lastName)) &&
                (flightNumber is null || StringUtils.EqualsIgnoreCase(f.FlightNumber, flightNumber)) &&
                (wantedClass is null || b.Class == wantedClass) &&
                (!maxPrice.HasValue || b.Price <= maxPrice.Value) &&
                (departureCountry is null || StringUtils.EqualsIgnoreCase(f.DepartureCountry, departureCountry)) &&
                (destinationCountry is null ||
                 StringUtils.EqualsIgnoreCase(f.DestinationCountry, destinationCountry)) &&
                (departureAirport is null || StringUtils.EqualsIgnoreCase(f.DepartureAirport, departureAirport)) &&
                (arrivalAirport is null || StringUtils.EqualsIgnoreCase(f.ArrivalAirport, arrivalAirport)) &&
                (!departureDate.HasValue || DateOnly.FromDateTime(depUtc) == departureDate.Value);
        }

        return bookings
            .Join(
                flights,
                b => b.FlightId,
                f => f.Id,
                (b, f) => new { b, f })
            .Join(passengers,
                bf => bf.b.PassengerId,
                p => p.Id,
                (bf, p) => new { bf.b, bf.f, p })
            .Where(x => Matches(x.b, x.f, x.p))
            .OrderByDescending(x => x.b.CreatedAt)
            .Select(x => new BookingSearchView(
                x.b,
                x.f.FlightNumber,
                x.f.DepartureAirport,
                x.f.DepartureCountry,
                x.f.ArrivalAirport,
                x.f.DestinationCountry,
                x.f.DepartureDate,
                x.f.ArrivalDate,
                x.p.FirstName,
                x.p.LastName
            ));
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