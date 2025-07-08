namespace VendingMachine.Data.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public int PriceInStotinki { get; set; }
    public byte Quantity { get; set; }
}
