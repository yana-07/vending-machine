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
                case VendorAction.View_Products:
                    tablePrinter.Print(await productService
                        .GetAllAsNoTrackingAsync());
                    break;
                case VendorAction.View_Coins:
                    tablePrinter.Print(await coinService
                        .GetAllAsNoTrackingAsync());
                    break;
            }

            if (selectedAction == VendorAction.Cancel)
            {
                Console.Clear();
                Console.WriteLine("\x1b[3J");
                break;
            }

            await ProcessVendorActionAsync(selectedAction);
        }
    }

    private VendorAction RequestAction()
    {
        return ansiConsole.Prompt(
            new SelectionPrompt<VendorAction>().Title("Select action:")
                .UseConverter(action => action.ToString().Replace('_', ' '))
                .AddChoices(Enum.GetValues<VendorAction>()));
    }

    private async Task ProcessVendorActionAsync(
        VendorAction action)
    {
        if (action == VendorAction.Update_Product_Quantity)
        {
            await UpdateProductQuantityAsync();
        }
        else if (action == VendorAction.Update_Product_Price)
        {
            await UpdateProductPriceAsync();
        }
        else if (action == VendorAction.Add_Product)
        {
            await AddProductAsync();
        }
        else if (action == VendorAction.Remove_Product)
        {
            await RemoveProductAsync();
        }
        else if (action == VendorAction.Deposit_Coins)
        {
            await DepositCoinsAsync();
        }
        else if (action == VendorAction.Collect_Coins)
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
                nameof(VendorCommand.Back)];

            var selection = PromptSelection("Select product code:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var product = products.First(product => product.Code == selection);

            var quantity = ansiConsole.Prompt(
                new TextPrompt<byte>($"Enter new quantity for " +
                    $"\"{product.Name}\" with code {selection}:")
                .Validate(input =>
                    input >= ProductConstants.MinQuantity &&
                    input <= ProductConstants.MaxQuantity)
                .ValidationErrorMessage($"[red]Quantity must be a number between " +
                    $"{ProductConstants.MinQuantity} and {ProductConstants.MaxQuantity}.[/]"));


            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the quantity of \"{product.Name}\" with code {selection} to {quantity}?"))
            {
                try
                { 
                    await productService.UpdateQuantityAsync(
                        new ProductQuantityUpdateDto
                        {
                            Code = selection,
                            Quantity = quantity
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
                nameof(VendorCommand.Back)];

            var selection = PromptSelection("Select product code:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var product = products.First(product => product.Code == selection);

            var price = ansiConsole.Prompt(
                new TextPrompt<decimal>($"Enter new price (in leva) " +
                    $"for \"{product.Name}\" with code {selection}:")
                .Validate(input =>
                {
                    if (input < 0)
                        return ValidationResult.Error(
                            "[red]Price cannot be negative.[/]");

                    if (input * 100 > int.MaxValue)
                        return ValidationResult.Error(
                            "[red]Price is too large.[/]");
                     
                    return ValidationResult.Success();
                })
                .ValidationErrorMessage("[red]Invalid price.[/]"));

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
                new TextPrompt<decimal>("Enter price (in leva)")
                .Validate(input =>
                {
                    if (input < 0)
                        return ValidationResult.Error(
                            "[red]Price cannot be negative.[/]");

                    if (input * 100 > int.MaxValue)
                        return ValidationResult.Error(
                            "[red]Price is too large.[/]");

                    return ValidationResult.Success();
                })
                .ValidationErrorMessage("[red]Invalid price.[/]"));

            price = Math.Round(price, 2);

            var quantity = ansiConsole.Prompt(
                new TextPrompt<byte>("Enter product quantity:")
                .ValidationErrorMessage("[red]Invalid quantity.[/]")
                .Validate(input =>
                    input >= ProductConstants.MinQuantity &&
                    input <= ProductConstants.MaxQuantity)
                .ValidationErrorMessage($"[red]Quantity must be a number between " +
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
                        PriceInStotinki = (int)(price * 100),
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

            string[] choices = [
                "Add new product",
                nameof(VendorCommand.Back)];

            var nextAction = PromptSelection("What would you like to do next?", choices);

            if (nextAction == nameof(VendorCommand.Back)) break;
        }
    }

    private async Task RemoveProductAsync()
    {
        while (true)
        {
            var products = await productService.GetAllAsNoTrackingAsync();

            string[] choices = [
                .. products.Select(product => product.Code),
                nameof(VendorCommand.Back)];

            var selection = PromptSelection("Select product code:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

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
        var coins = await coinService.GetAllAsNoTrackingAsync();

        string[] choices = [
            .. coins.Select(coin => coin.Denomination),
            nameof(VendorCommand.Back)];

        while (true)
        {
            var selection = PromptSelection("Deposit coin:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:")
                    .Validate(input => input > 0,
                        "[red]Quantity must be greater than 0.[/]"));

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
        var coins = await coinService.GetAllAsNoTrackingAsync();

        string[] choices = [
            .. coins.Select(coin => coin.Denomination),
            nameof(VendorCommand.Back)];

        while (true)
        {
            var selection = PromptSelection("Collect coins:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var coinValue = coinService.ParseCoinValue(selection);

            if (CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:")
                    .Validate(input => input > 0,
                        "[red]Quantity must be greater than 0.[/]"));

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

    private string PromptSelection(
        string title, IEnumerable<string> choices)
    {
        return ansiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(choices));
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
