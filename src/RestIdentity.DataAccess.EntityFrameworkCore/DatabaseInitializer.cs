using Microsoft.Extensions.DependencyInjection;
using RestIdentity.DataAccess.Data;

namespace RestIdentity.DataAccess.EntityFrameworkCore;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ApplicationDbContext _applicationDbContext;

    public DatabaseInitializer(IServiceProvider serviceProvider)
    {
        _applicationDbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public Task<bool> EnsureCreatedAsync()
    {
        return _applicationDbContext.Database.EnsureCreatedAsync();
    }
}