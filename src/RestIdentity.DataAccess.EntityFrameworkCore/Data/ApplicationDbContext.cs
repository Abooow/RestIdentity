using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Data;

public sealed class ApplicationDbContext : IdentityDbContext<UserDao>
{
    public DbSet<TokenDao> Tokens { get; set; }
    public DbSet<AuditLogDao> AuditLogs { get; set; }
    public DbSet<UserAvatarDao> UserAvatars { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityRole>().HasData(
            new() { Id = RolesConstants.AdminId, Name = RolesConstants.Admin, NormalizedName = RolesConstants.AdminNormalized },
            new() { Id = RolesConstants.CustomerId, Name = RolesConstants.Customer, NormalizedName = RolesConstants.CustomerNormalized });

        builder.Entity<AuditLogDao>()
            .HasIndex(b => b.UserId);

        builder.Entity<UserDao>(entity =>
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
