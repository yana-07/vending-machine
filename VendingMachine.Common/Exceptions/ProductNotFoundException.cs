namespace VendingMachine.Common.Exceptions;

public class ProductNotFoundException : Exception
{
    public string? ProductCode { get; }

    public ProductNotFoundException() 
    {
    }

    public ProductNotFoundException(string productCode)
        : base($"Product with code {productCode} does not exist.")
    {
        ProductCode = productCode;
    }
}
