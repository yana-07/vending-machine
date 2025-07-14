using Microsoft.Extensions.Logging;
using Spectre.Console;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Enums;
using VendingMachine.Common.Exceptions;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class VendorService(
    IAnsiConsole ansiConsole,
    IProductService productService,
    ICoinService coinService,
    ITablePrinter tablePrinter,
    ICurrencyFormatter currencyFormatter,
    ILogger<VendorService> logger) :
    IVendorService
{
    public async Task ServeVendorAsync()
    {
        while (true)
        {
            var selectedAction = RequestAction();
            switch (selectedAction)
            {
                case VendorActions.View_Products:
                    tablePrinter.Print(await productService
                        .GetAllAsNoTrackingAsync());
                    break;
                case VendorActions.View_Coins:
                    tablePrinter.Print(await coinService
                        .GetAllDescendingAsNoTrackingAsync());
                    break;
            }

            if (selectedAction == VendorActions.Cancel)
            {
                Console.Clear();
                Console.WriteLine("\x1b[3J");
                break;
            }

            await ProcessVendorActionAsync(selectedAction);
        }
    }

    private VendorActions RequestAction()
    {
        return ansiConsole.Prompt(
            new SelectionPrompt<VendorActions>().Title("Select action:")
                .UseConverter(action => action.ToString().Replace('_', ' '))
                .AddChoices(Enum.GetValues<VendorActions>()));
    }

    private async Task ProcessVendorActionAsync(
        VendorActions action)
    {
        if (action == VendorActions.Update_Product_Quantity)
        {
            await UpdateProductQuantityAsync();
        }
        else if (action == VendorActions.Update_Product_Price)
        {
            await UpdateProductPriceAsync();
        }
        else if (action == VendorActions.Add_Product)
        {
            await AddProductAsync();
        }
        else if (action == VendorActions.Remove_Product)
        {
            await RemoveProductAsync();
        }
        else if (action == VendorActions.Deposit_Coins)
        {
            await DepositCoinsAsync();
        }
        else if (action == VendorActions.Collect_Coins)
        {
            await CollectCoinsAsync();
        }
    }

    private async Task UpdateProductQuantityAsync()
    {
        while (true)
        {
            var products = await productService.GetAllAsNoTrackingAsync();

            string[] choices = [
                .. products.Select(product => product.Code),
                nameof(VendorCommands.Back)];

            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select product code:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            var product = products.First(product => product.Code == selection);

            var newQuantity = ansiConsole.Prompt(
                new TextPrompt<byte>($"Enter new quantity for " +
                    $"\"{product.Name}\" with code {selection}:")
                .ValidationErrorMessage("[red]That's not a valid product quantity.[/]")
                .Validate(quantity =>
                    quantity > ProductConstants.MinQuantity &&
                    quantity <= ProductConstants.MaxQuantity,
                    $"[red]The product quantity must be between " +
                    $"{ProductConstants.MinQuantity} and {ProductConstants.MaxQuantity}.[/]"));

            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the quantity of \"{product.Name}\" with code {selection} to {newQuantity}?"))
            {
                try
                { 
                    await productService.UpdateQuantityAsync(
                        new ProductQuantityUpdateDto
                        {
                            Code = selection,
                            Quantity = newQuantity
                        });

                    ansiConsole.MarkupLine("[green]Quantity successfully updated.[/]");
                }
                catch (Exception ex) 
                    when (ex is InvalidOperationException || 
                        ex is ProductNotFoundException)
                {
                    logger.LogError(ex, nameof(UpdateProductQuantityAsync));
                    ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                }
            }
        }
    }

    private async Task UpdateProductPriceAsync()
    {
        while (true)
        {
            var products = await productService.GetAllAsNoTrackingAsync();

            string[] choices = [
                .. products.Select(product => product.Code),
                nameof(VendorCommands.Back)];

            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select product code:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            var product = products.First(product => product.Code == selection);

            var price = ansiConsole.Prompt(
                new TextPrompt<decimal>($"Enter new price (in leva) " +
                    $"for \"{product.Name}\" with code {selection}:")
                .Validate(input =>
                {
                    return input * 100 > int.MaxValue ?
                        ValidationResult.Error("[red]Price is too large.[/]") :
                        ValidationResult.Success();
                })
                .ValidationErrorMessage("[red]Invalid product price.[/]"));

            price = Math.Round(price, 2);

            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the price of \"{product.Name}\" with code {selection} to {price}" +
                $"{CurrencyConstants.LevaSuffix}?"))
            {
                try
                {
                    await productService.UpdatePriceAsync(
                        new ProductPriceUpdateDto
                        {
                            Code = selection,
                            Price = (int)(price * 100)
                        });
                
                    ansiConsole.MarkupLine("[green]Price successfully updated.[/]");
                }
                catch (Exception ex)
                    when (ex is InvalidOperationException ||
                        ex is ProductNotFoundException)
                {
                    logger.LogError(ex, nameof(UpdateProductPriceAsync));
                    ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                }
            }
        }
    }

    private async Task AddProductAsync()
    {
        while (true)
        {
            if (!await productService.CanAddAsync())
            {
                ansiConsole.MarkupLine("[red]No free slots available.[/]");
                return;
            }

            var code = ansiConsole.Prompt(
                new TextPrompt<string>($"Enter product code:"));

            var name = ansiConsole.Prompt(
                new TextPrompt<string>("Enter product name:"));

            var price = ansiConsole.Prompt(
                new TextPrompt<decimal>("Enter new price (in leva)")
                .Validate(input =>
                {
                    return input * 100 > int.MaxValue ?
                        ValidationResult.Error("[red]Price is too large.[/]") :
                        ValidationResult.Success();
                })
                .ValidationErrorMessage("[red]Invalid product price.[/]"));

            price = Math.Round(price, 2);

            var quantity = ansiConsole.Prompt(
                new TextPrompt<byte>("Enter product quantity:")
                .ValidationErrorMessage("[red]Invalid product quantity.[/]")
                .Validate(quantity =>
                    quantity > ProductConstants.MinQuantity &&
                    quantity <= ProductConstants.MaxQuantity,
                    $"[red]The quantity must be between " +
                    $"{ProductConstants.MinQuantity} and {ProductConstants.MaxQuantity}.[/]"));

            if (IsActionConfirmed(
                $"Code: {code}{Environment.NewLine}" +
                $"Name: {name}{Environment.NewLine}" +
                $"Quantity: {quantity}{Environment.NewLine}" +
                $"Price: {price}{CurrencyConstants.LevaSuffix}{Environment.NewLine}" +
                $"Are you sure you want to add this product?"))
            {
                try
                {
                    await productService.AddAsync(new ProductDto
                    {
                        Code = code,
                        Name = name,
                        Price = (int)(price * 100),
                        Quantity = quantity
                    });

                    ansiConsole.MarkupLine("[green]Product successfully added.[/]");
                }
                catch (InvalidOperationException ex)
                {
                    logger.LogError(ex, nameof(AddProductAsync));
                    ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                }
            }

            var nextAction = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("What would you like to do next?")
                .AddChoices([
                    "Add new product",
                    nameof(VendorCommands.Back)]));

            if (nextAction == nameof(VendorCommands.Back)) break;
        }
    }

    private async Task RemoveProductAsync()
    {
        while (true)
        {
            var products = await productService.GetAllAsNoTrackingAsync();

            string[] choices = [
                .. products.Select(product => product.Code),
                nameof(VendorCommands.Back)];

            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Select product code:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            var product = products.First(product => product.Code == selection);

            if (IsActionConfirmed($"Are you sure you want to remove " +
                $"product \"{product.Name}\" with code {selection}?"))
            {
                try
                {
                    await productService.RemoveAsync(selection);
                    ansiConsole.MarkupLine("[green]Product successfully removed.[/]");
                }
                catch (ProductNotFoundException ex)
                {
                    logger.LogError(ex, nameof(RemoveProductAsync));
                    AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
                }
            }
        }
    }

    private async Task DepositCoinsAsync()
    {
        string[] choices = [
            .. CoinConstants.AllowedCoins.Select(
                coin => currencyFormatter.FormatCoinValue(coin)),
            nameof(VendorCommands.Back)];

        while (true)
        {
            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Deposit coin:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:"));

                if (IsActionConfirmed($"Are you sure you want " +
                    $"to deposit {quantity}x{selection}?"))
                {
                    try
                    {
                        await coinService.DepositAsync(
                            new CoinDto
                            {
                                Value = coinValue,
                                Quantity = quantity
                            });

                        var coinsLabel = quantity > 1 ? "Coins" : "Coin";

                        ansiConsole.MarkupLine($"[green]{coinsLabel} successfully deposited.[/]");
                    }
                    catch (CoinNotFoundException ex)
                    {
                        logger.LogError(ex, nameof(DepositCoinsAsync));
                        ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    }
                }
            }
        }
    }

    private async Task CollectCoinsAsync()
    {
        string[] choices = [
            .. CoinConstants.AllowedCoins.Select(
                coin => currencyFormatter.FormatCoinValue(coin)),
            nameof(VendorCommands.Back)];

        while (true)
        {
            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Collect coins:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:"));

                if (IsActionConfirmed($"Are you sure you want " +
                    $"to collect {quantity}x{selection}?"))
                {
                    try
                    {
                        await coinService.DecreaseInventoryAsync(
                            new CoinDto
                            {
                                Value = coinValue,
                                Quantity = quantity
                            });

                        ansiConsole.MarkupLine($"[green]Coins successfully collected.[/]");
                    }
                    catch (Exception ex) when (
                        ex is CoinNotFoundException || 
                        ex is InvalidOperationException)
                    {
                        logger.LogError(ex, nameof(CollectCoinsAsync));
                        ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
                    }
                }
            }
        }
    }

    private bool IsActionConfirmed(string message)
    {
        return ansiConsole.Prompt(
            new TextPrompt<bool>(message)
                .AddChoice(true)
                .AddChoice(false)
                .WithConverter(choice => choice ? "y" : "n"));
    }
}
