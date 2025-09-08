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
        var passengerService = new PassengerService(passengerRepository);

        var mainMenu = new MainMenu(passengerService);
        await mainMenu.RunAsync();
    }
}