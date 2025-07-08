namespace VendingMachine.Common.Exceptions;

public class CoinNotFoundException : Exception
{
    public byte CoinValue { get; }

    public CoinNotFoundException()
    {
    }

    public CoinNotFoundException(
        string message)
        : base(message)
    {
    }

    public CoinNotFoundException(
        string message, byte coinValue)
        : base(message)
    {
        CoinValue = coinValue;
    }
}
