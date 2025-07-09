namespace VendingMachine.Services.DTOs;

public class ProductRequestResultDto
{
    public bool IsCancelled { get; init; }
    public bool IsValid { get; init; }
    public required string ProductCode { get; init; }
}
