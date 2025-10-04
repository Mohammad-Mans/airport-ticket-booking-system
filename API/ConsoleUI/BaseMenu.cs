namespace ATBS.API.ConsoleUI;

public abstract class BaseMenu
{
    protected abstract void DisplayMenu();
    protected abstract Task<bool> HandleChoiceAsync(string choice);

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            DisplayMenu();

            Console.Write("\nEnter your choice: ");
            var choice = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(choice))
                continue;

            if (choice == "0")
                break;

            try
            {
                var shouldContinue = await HandleChoiceAsync(choice);
                if (!shouldContinue)
                    break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    protected void DisplayHeader(string title)
    {
        Console.WriteLine("====================================");
        Console.WriteLine($"   {title}");
        Console.WriteLine("====================================");
    }

    protected void WaitForKeyPress()
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
}