using RestIdentity.Server.Data;

namespace RestIdentity.DataAccess.EntityFrameworkCore;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly ApplicationDbContext _applicationDbContext;

    public DatabaseInitializer(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public Task<bool> EnsureCreatedAsync()
    {
        return _applicationDbContext.Database.EnsureCreatedAsync();
    }
}