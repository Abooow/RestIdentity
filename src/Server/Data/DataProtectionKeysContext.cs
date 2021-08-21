using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RestIdentity.Server.Data;

public sealed class DataProtectionKeysContext : DbContext, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DataProtectionKeysContext(DbContextOptions<DataProtectionKeysContext> options)
    : base(options)
    {
    }
}
