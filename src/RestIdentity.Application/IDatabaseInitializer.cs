namespace RestIdentity.DataAccess;

public interface IDatabaseInitializer
{
    Task<bool> EnsureCreatedAsync();
}
