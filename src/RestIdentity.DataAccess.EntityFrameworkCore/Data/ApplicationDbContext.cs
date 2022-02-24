using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Data;

internal sealed class ApplicationDbContext : IdentityDbContext<UserRecord>
{
    public DbSet<TokenRecord> Tokens { get; set; }
    public DbSet<AuditLogRecord> AuditLogs { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AuditLogRecord>()
            .HasIndex(b => b.UserId);

        builder.Entity<UserRecord>(entity =>
        {
            entity.ToTable(name: "Users", "Identity");
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable(name: "Roles", "Identity");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles", "Identity");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims", "Identity");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins", "Identity");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable(name: "RoleClaims", "Identity");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens", "Identity");
        });
    }
}
