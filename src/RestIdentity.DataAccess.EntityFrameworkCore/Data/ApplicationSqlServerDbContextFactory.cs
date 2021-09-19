using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using RestIdentity.Server.Data;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Data;

public class ApplicationSqlServerDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        string connectionString = args.FirstOrDefault() ?? throw new ArgumentException("Pass the connection string as the first argument, use -args '[connectionString]'.");
        optionsBuilder.UseSqlServer(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
