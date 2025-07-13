namespace VendingMachine.Common.Exceptions;

public class CoinNotFoundException : Exception
{
    public byte CoinValue { get; }

    public CoinNotFoundException()
    {
    }

    public CoinNotFoundException(byte coinValue)
        : base($"Coin with value {coinValue} does not exist.")
    {
        CoinValue = coinValue;
    }
}
