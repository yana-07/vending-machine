using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VendingMachine.Data;
using VendingMachine.Seeder;

using var host = CreateHostBuilder(args).Build();

using var serviceScope = host.Services.CreateScope();
var serviceProvider = serviceScope.ServiceProvider;

var dbContext = serviceProvider
    .GetRequiredService<VendingMachineDbContext>();
await dbContext.Database.MigrateAsync();

var seederCoordinator = serviceProvider
    .GetRequiredService<SeederCoordinator>();
await seederCoordinator.SeedAllAsync();

Console.ReadKey();

static IHostBuilder CreateHostBuilder(string[] args)
{
    var hostBuilder = Host.CreateDefaultBuilder(args)
        .ConfigureServices((context, services) =>
        {
            services.AddDbContext<VendingMachineDbContext>(
                options => options.UseSqlite(
                    context.Configuration
                    .GetConnectionString("DefaultConnection")))
            .AddScoped<IDataSeeder, ProductDataSeeder>()
            .AddScoped<IDataSeeder, CoinDataSeeder>()
            .AddScoped<SeederCoordinator>();
        });

    return hostBuilder;
}