using Spectre.Console;
using VendingMachine.Common.Constants;
using VendingMachine.Common.Enums;
using VendingMachine.Common.Helpers;
using VendingMachine.Services.DTOs;

namespace VendingMachine.Services.Services;

public class VendorService(
    IAnsiConsole ansiConsole,
    IProductService productService,
    ICoinService coinService,
    ITablePrinter tablePrinter) :
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

            var product = products.FirstOrDefault(product => product.Code == selection);

            if (product is null)
            {
                ansiConsole.MarkupLine("[red]Invalid product code.[/]");
                continue;
            }

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
                await productService.UpdateQuantityAsync(
                    new ProductQuantityUpdateDto
                    {
                        Code = selection,
                        Quantity = newQuantity
                    });

                ansiConsole.MarkupLine("[green]Quantity successfully updated.[/]");
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

            var product = products.FirstOrDefault(product => product.Code == selection);

            if (product is null)
            {
                ansiConsole.MarkupLine("[red]Invalid product code.[/]");
                continue;
            }

            var newPrice = ansiConsole.Prompt(
                new TextPrompt<int>($"Enter new price (in stotinki) " +
                    $"for \"{product.Name}\" with code {selection}:")
                .ValidationErrorMessage("[red]Invalid product price.[/]"));

            if (IsActionConfirmed($"Are you sure you want to update " +
                $"the price of \"{product.Name}\" with code {selection} to {newPrice}st?"))
            {
                await productService.UpdatePriceAsync(
                    new ProductPriceUpdateDto
                    {
                        Code = selection,
                        Price = newPrice
                    });

                ansiConsole.MarkupLine("[green]Price successfully updated.[/]");
            }
        }
    }

    private async Task AddProductAsync()
    {
        while (true)
        {
            if (!await productService.CanAddAsync()) break;

            var code = ansiConsole.Prompt(
                new TextPrompt<string>($"Enter product code:"));

            var name = ansiConsole.Prompt(
                new TextPrompt<string>("Enter product name:"));

            var price = ansiConsole.Prompt(
                new TextPrompt<int>("Enter product price (in stotinki):"));

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
                $"Price: {price}st{Environment.NewLine}" +
                $"Are you sure you want to add this product?"))
            {
                await productService.AddAsync(new ProductDto
                {
                    Code = code,
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });

                ansiConsole.MarkupLine("[green]Product successfully added.[/]");
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

            var product = products.FirstOrDefault(product => product.Code == selection);

            if (product is null)
            {
                ansiConsole.MarkupLine("[red]Invalid product code.[/]");
                continue;
            }

            if (IsActionConfirmed($"Are you sure you want to remove " +
                $"product \"{product.Name}\" with code {selection}?"))
            {
                await productService.RemoveAsync(selection);

                ansiConsole.MarkupLine("[green]Product successfully removed.[/]");
            }
        }
    }

    private async Task DepositCoinsAsync()
    {
        string[] choices = [
            .. CoinConstants.AllowedCoins.Select(coin => coin.ToString()),
            nameof(VendorCommands.Back)];

        while (true)
        {
            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Deposit coin:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            if (byte.TryParse(selection, out byte coinValue) &&
                CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:"));

                if (IsActionConfirmed($"Are you sure you want " +
                    $"to deposit {quantity}x{selection}st?"))
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
            }
        }
    }

    private async Task CollectCoinsAsync()
    {
        string[] choices = [
            .. CoinConstants.AllowedCoins.Select(coin => coin.ToString()),
            nameof(VendorCommands.Back)];

        while (true)
        {
            var selection = ansiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Collect coins:")
                    .AddChoices(choices));

            if (selection == nameof(VendorCommands.Back)) break;

            if (byte.TryParse(selection, out byte coinValue) &&
                CoinConstants.AllowedCoins.Contains(coinValue))
            {
                var quantity = ansiConsole.Prompt(
                    new TextPrompt<int>("Enter coin quantity:"));

                if (IsActionConfirmed($"Are you sure you want " +
                    $"to collect {quantity}x{selection}st?"))
                {
                    await coinService.DecreaseInventoryAsync(
                        new CoinDto
                        {
                            Value = coinValue,
                            Quantity = quantity
                        });

                    ansiConsole.MarkupLine($"[green]Coins successfully collected.[/]");
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
