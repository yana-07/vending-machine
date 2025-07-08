namespace VendingMachine.Services.Services;

public interface ICoinService
{
    Task Insert(byte value);
    Task ReturnInserted(byte value);
}
