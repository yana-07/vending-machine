using VendingMachine.Services.Services;

namespace VendingMachine.ConsoleApp.UserInteraction;

public class ConsoleUserInteractor : IUserInteractor
{
    public string? ReadInput()
    {
        var input = Console.ReadLine();
        Console.WriteLine();
        return input;
    }

    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
        Console.WriteLine();
    }
        
}
