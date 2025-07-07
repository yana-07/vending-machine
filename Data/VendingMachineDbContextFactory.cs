using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace VendingMachine.Data;

public class VendingMachineDbContextFactory
    : IDesignTimeDbContextFactory<VendingMachineDbContext>
{
    public VendingMachineDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<VendingMachineDbContext>();
        optionsBuilder.UseSqlite(configuration.GetConnectionString("DefaultConnection"));

        return new VendingMachineDbContext(optionsBuilder.Options);
    }
}
