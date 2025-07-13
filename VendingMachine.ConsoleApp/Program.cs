using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VendingMachine.Data.Context;
using VendingMachine.Data.Seed;
using VendingMachine.ConsoleApp.App;
using VendingMachine.Services.Services;
using VendingMachine.Common.Helpers;
using Spectre.Console;
using Serilog;
using VendingMachine.Data.Repositories;
using VendingMachine.Data.Models;

using var host = CreateHostBuilder(args).Build();

using var serviceScope = host.Services.CreateScope();
var serviceProvider = serviceScope.ServiceProvider;

var dbContext = serviceProvider
    .GetRequiredService<VendingMachineDbContext>();
await dbContext.Database.MigrateAsync();

var seederCoordinator = serviceProvider
    .GetRequiredService<SeederCoordinator>();
await seederCoordinator.SeedAllAsync();

var vendingMachineApp = serviceProvider
    .GetRequiredService<IVendingMachineApp>();
await vendingMachineApp.RunAsync();

static IHostBuilder CreateHostBuilder(string[] args)
{
    var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<VendingMachineDbContext>(
                options => options.UseSqlite(
                    context.Configuration
                    .GetConnectionString("DefaultConnection")))
            .AddScoped<IVendingMachineApp, VendingMachineApp>()
            .AddScoped<IDataSeeder, ProductDataSeeder>()
            .AddScoped<IDataSeeder, CoinDataSeeder>()
            .AddScoped<SeederCoordinator>()
            .AddScoped<IUserService, UserService>()
            .AddScoped<IProductService, ProductService>()
            .AddScoped<ICoinService, CoinService>()
            .AddScoped<IChangeService, ChangeService>()
            .AddScoped<ICustomerService, CustomerService>()
            .AddScoped<IVendorService, VendorService>()
            .AddScoped<ITablePrinter, TablePrinter>()
            .AddScoped<IRepository<Product>, Repository<Product>>()
            .AddScoped<IRepository<Coin>, Repository<Coin>>()
            .AddScoped(_ => AnsiConsole.Console);
        });

    var config = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build();

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .ReadFrom.Configuration(config)
        .CreateLogger();

    hostBuilder.UseSerilog();

    return hostBuilder;
}