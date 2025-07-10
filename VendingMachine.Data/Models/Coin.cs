using Microsoft.EntityFrameworkCore;

namespace VendingMachine.Data.Models;

[Index(nameof(Value), IsUnique = true)]
public class Coin
{
    public int Id { get; set; }

    public byte Value { get; set; }

    public int Quantity { get; set; }
}
