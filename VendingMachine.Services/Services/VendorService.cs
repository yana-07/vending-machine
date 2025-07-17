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

            var selection = PromptForSelection("Select product code:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var product = products.First(product => product.Code == selection);

            var quantity = PromptForProductQuantity(product.Name, selection);

            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the quantity of \"{product.Name}\" with code {selection} to {quantity}?"))
            {
                try
                { 
                    var operationResult = await productService
                        .UpdateQuantityAsync(
                            new ProductQuantityUpdateDto
                            {
                                Code = selection,
                                Quantity = quantity
                            });

                    if (!operationResult.IsSuccess)
                    {
                        ansiConsole.MarkupLine($"[red]{operationResult.ErrorMessage}[/]");
                    }
                    else
                    {
                        ansiConsole.MarkupLine("[green]Quantity successfully updated.[/]");
                    }
                }
                catch (ProductNotFoundException ex)
                {
                    LogError(ex, nameof(UpdateProductQuantityAsync));
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

            var selection = PromptForSelection("Select product code:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            var product = products.First(product => product.Code == selection);

            var price = PromptForPrice(product.Name, selection);

            price = Math.Round(price, 2);

            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the price of \"{product.Name}\" with code {selection} to {price}" +
                $"{CurrencyConstants.LevaSuffix}?"))
            {
                try
                {
                    var operationResult = await productService
                        .UpdatePriceAsync(
                            new ProductPriceUpdateDto
                            {
                                Code = selection,
                                Price = (int)(price * 100)
                            });

                    if (!operationResult.IsSuccess)
                    {
                        ansiConsole.MarkupLine($"[red]{operationResult.ErrorMessage}[/]");
                    }
                    else
                    {
                        ansiConsole.MarkupLine("[green]Price successfully updated.[/]");
                    }
                }
                catch (ProductNotFoundException ex)
                {
                    LogError(ex, nameof(UpdateProductPriceAsync));
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

            var price = PromptForPrice();

            price = Math.Round(price, 2);

            var quantity = PromptForProductQuantity();

            if (IsActionConfirmed(
                $"Code: {code}{Environment.NewLine}" +
                $"Name: {name}{Environment.NewLine}" +
                $"Quantity: {quantity}{Environment.NewLine}" +
                $"Price: {price}{CurrencyConstants.LevaSuffix}{Environment.NewLine}" +
                $"Are you sure you want to add this product?"))
            {

                var operationResult = await productService
                    .AddAsync(
                        new ProductDto
                        {
                            Code = code,
                            Name = name,
                            PriceInStotinki = (int)(price * 100),
                            Quantity = quantity
                        });

                if (!operationResult.IsSuccess)
                {
                    ansiConsole.MarkupLine($"[red]{operationResult.ErrorMessage}[/]");
                }
                else
                {
                    ansiConsole.MarkupLine("[green]Product successfully added.[/]");
                }
            }

            string[] choices = [
                "Add new product",
                nameof(VendorCommand.Back)];

            var nextAction = PromptForSelection("What would you like to do next?", choices);

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

            var selection = PromptForSelection("Select product code:", choices);

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
                    LogError(ex, nameof(RemoveProductAsync));
                }
            }
        }
    }

    private async Task DepositCoinsAsync()
    {
        var coinDenominationToValueMap = await coinService
            .GetAllAsDenominationToValueMap();

        string[] choices = [
            .. coinDenominationToValueMap.Keys,
            nameof(VendorCommand.Back)];

        while (true)
        {
            var selection = PromptForSelection("Deposit coin:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            if (!coinDenominationToValueMap.TryGetValue(selection, out var coinValue))
            {
                ansiConsole.MarkupLine($"[red]Invalid coin selection: {selection}[/]");
                continue;
            }

            var quantity = PromptForCoinQuantity();

            if (IsActionConfirmed($"Are you sure you want " +
                $"to deposit {quantity}x{selection}?"))
            {
                try
                {
                    await coinService.DepositAsync(coinValue, quantity);

                    var coinsLabel = quantity > 1 ? "Coins" : "Coin";

                    ansiConsole.MarkupLine($"[green]{coinsLabel} successfully deposited.[/]");
                }
                catch (CoinNotFoundException ex)
                {
                    LogError(ex, nameof(DepositCoinsAsync));                       
                }
            }
        }
    }

    private async Task CollectCoinsAsync()
    {
        var coinDenominationToValueMap = await coinService
            .GetAllAsDenominationToValueMap();

        string[] choices = [
            .. coinDenominationToValueMap.Keys,
            nameof(VendorCommand.Back)];

        while (true)
        {
            var selection = PromptForSelection("Collect coins:", choices);

            if (selection == nameof(VendorCommand.Back)) break;

            if (!coinDenominationToValueMap.TryGetValue(selection, out var coinValue))
            {
                ansiConsole.MarkupLine($"[red]Invalid coin selection: {selection}[/]");
                continue;
            }

            var quantity = PromptForCoinQuantity();

            if (IsActionConfirmed($"Are you sure you want " +
                $"to collect {quantity}x{selection}?"))
            {
                try
                {
                    var operationResult = await coinService
                        .DecreaseInventoryAsync(coinValue, quantity);

                    if (operationResult.IsSuccess)
                    {
                        ansiConsole.MarkupLine($"[green]Coins successfully collected.[/]");
                    }
                    else
                    {
                        ansiConsole.MarkupLine($"[red]{operationResult.ErrorMessage}[/]");
                    }
                }
                catch (CoinNotFoundException ex)
                {
                    LogError(ex, nameof(CollectCoinsAsync));
                }
            }
        }
    }

    private string PromptForSelection(
        string title, IEnumerable<string> choices)
    {
        return ansiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(title)
                .AddChoices(choices));
    }

    private byte PromptForProductQuantity(
    string? productName = null, string? productCode = null)
    {
        string productDescription = string.Empty;

        if (!string.IsNullOrEmpty(productName) &&
            !string.IsNullOrEmpty(productCode))
        {
            productDescription = $" for \"{productName}\" " +
                $"with code {productCode}";
        }

        return ansiConsole.Prompt(
            new TextPrompt<byte>($"Enter quantity{productDescription}:")
            .Validate(input =>
                input >= ProductConstants.MinQuantity &&
                input <= ProductConstants.MaxQuantity)
            .ValidationErrorMessage($"[red]Quantity must be a number between " +
                $"{ProductConstants.MinQuantity} and {ProductConstants.MaxQuantity}.[/]"));
    }

    private decimal PromptForPrice(
        string? productName = null, string? productCode = null)
    {
        string productDescription = string.Empty;

        if (!string.IsNullOrEmpty(productName) &&
            !string.IsNullOrEmpty(productCode))
        {
            productDescription = $" for \"{productName}\" " +
                $"with code {productCode}";
        }

        return ansiConsole.Prompt(
            new TextPrompt<decimal>($"Enter price (in leva){productDescription}:")
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
    }

    private int PromptForCoinQuantity()
    {
        return ansiConsole.Prompt(
            new TextPrompt<int>("Enter coin quantity:")
            .Validate(input => input > 0,
                "[red]Quantity must be greater than 0.[/]"));
    }

    private bool IsActionConfirmed(string message)
    {
        return ansiConsole.Prompt(
            new TextPrompt<bool>(message)
                .AddChoice(true)
                .AddChoice(false)
                .WithConverter(choice => choice ? "y" : "n"));
    }

    private void LogError(Exception ex, string methodName)
    {
        logger.LogError(ex, "An error occurred in {Method}.", methodName);
        ansiConsole.MarkupLine($"[red]{ex.Message}[/]");
    }
}
