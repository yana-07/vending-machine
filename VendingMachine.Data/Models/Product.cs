using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using VendingMachine.Common.Constants;

namespace VendingMachine.Data.Models;

[Index(nameof(Code), IsUnique = true)]
public class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    [Range(0, int.MaxValue)]
    public int PriceInStotinki { get; set; }

    [Range(0, ProductConstants.MaxQuantity)]
    public byte Quantity { get; set; }
}
