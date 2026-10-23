using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CustomDataView.Infrastructure.Persistence;

public sealed class CustomDataViewDesignTimeDbContextFactory : IDesignTimeDbContextFactory<CustomDataViewDbContext>
{
    public CustomDataViewDbContext CreateDbContext(string[] args)
        => new CustomDataViewDbContext(new DbContextOptionsBuilder<CustomDataViewDbContext>()
            .UseNpgsql("Host=localhost", CustomDataViewDbContext.ConfigureNpgsql)
            .Options);
}
