using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Prison.Core;

namespace Prison.WpfApp
{
    public class DesignTimePrisonDbContextFactory : IDesignTimeDbContextFactory<PrisonDbContext>
    {
        public PrisonDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<PrisonDbContext>()
                .UseSqlServer(App.EfCoreConnectionString)
                .Options;

            return new PrisonDbContext(options);
        }
    }
}