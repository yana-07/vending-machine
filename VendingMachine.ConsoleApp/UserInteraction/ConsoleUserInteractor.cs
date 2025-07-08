using VendingMachine.Services.Services;

namespace VendingMachine.ConsoleApp.UserInteraction;

public class ConsoleUserInteractor : IUserInteractor
{
    public string? ReadInput() => 
        Console.ReadLine();

    public void ShowMessage(string message)
    {
        Console.WriteLine(message);
        Console.WriteLine();
    }
        
}
