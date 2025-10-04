using ATBS.API.ConsoleUI;
using ATBS.Data.Repositories;
using ATBS.Domain.Services;
using ATBS.Utils;

namespace ATBS;

class Program
{
    static async Task Main(string[] args)
    {
        var passengerRepository = new PassengerRepository(FilePaths.PassengerFilePath);
        var flightRepository = new FlightRepository(FilePaths.FlightFilePath);
        var flightClassRepository = new FlightClassRepository(FilePaths.FlightClassFilePath);
        var bookingRepository = new BookingRepository(FilePaths.BookingFilePath);

        var passengerService = new PassengerService(passengerRepository);
        var flightService = new FlightService(flightRepository, flightClassRepository);
        var bookingService = new BookingService(bookingRepository, passengerRepository, flightRepository,
            flightClassRepository);

        var mainMenu = new MainMenu(passengerService, flightService, bookingService);
        await mainMenu.RunAsync();
    }
}