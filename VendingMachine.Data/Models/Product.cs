using Microsoft.EntityFrameworkCore;

namespace VendingMachine.Data.Models;

[Index(nameof(Code), IsUnique = true)]
public class Product
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required string Name { get; set; }

    public int PriceInStotinki { get; set; }

    public byte Quantity { get; set; }
}
