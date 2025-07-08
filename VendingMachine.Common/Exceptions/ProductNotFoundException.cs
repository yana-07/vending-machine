namespace VendingMachine.Common.Exceptions;

public class ProductNotFoundException : Exception
{
    public string? ProductCode { get; }

    public ProductNotFoundException() 
    {
    }

    public ProductNotFoundException(
        string message) 
        : base(message) 
    { 
    }

    public ProductNotFoundException(
        string message, string productCode) 
        : base(message)
    {
        ProductCode = productCode;
    }
}
