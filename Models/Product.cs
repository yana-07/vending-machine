namespace VendingMachine.Models;

public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public byte Quantity { get; set; }
    public byte Code { get; set; }
}
