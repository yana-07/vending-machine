﻿namespace VendingMachine.Services.DTOs;

public class ProductPriceUpdateDto
{
    public required string Code { get; init; }

    public required int Price { get; init; }
}
