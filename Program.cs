using ATBS.API.ConsoleUI;

namespace ATBS;

class Program
{
    static async Task Main(string[] args)
    {
        var mainMenu = new MainMenu();
        await mainMenu.RunAsync();
    }
}