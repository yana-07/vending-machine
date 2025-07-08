using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VendingMachine.Data.Context;

public class VendingMachineDbContextFactory
    : IDesignTimeDbContextFactory<VendingMachineDbContext>
{
    public VendingMachineDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

        var optionsBuilder = new DbContextOptionsBuilder<VendingMachineDbContext>();
        optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));

        return new VendingMachineDbContext(optionsBuilder.Options);
    }
}
