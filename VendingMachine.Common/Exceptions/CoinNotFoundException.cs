namespace VendingMachine.Common.Exceptions;

public class CoinNotFoundException : Exception
{
    public byte CoinValue { get; }

    public IEnumerable<byte> CoinValues { get; } = [];

    public CoinNotFoundException()
    {
    }

    public CoinNotFoundException(byte coinValue)
        : base($"Coin with value {coinValue} does not exist.")
    {
        CoinValue = coinValue;
    }

    public CoinNotFoundException(IEnumerable<byte> coinValues)
        : base($"Coins with values {string.Join(", ", coinValues)} do not exist.")
    {
        CoinValues = coinValues;
    }
}
