namespace VendingMachine.Services.Services;

public interface IUserInteractor
{
    string? ReadInput();
    void ShowMessage(string message);
}
