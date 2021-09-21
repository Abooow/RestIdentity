using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess.Data;
using RestIdentity.DataAccess.Repositories;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _applicationDbContext;

    public RoleRepository(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public Task<bool> AnyRolesExistsAsync()
    {
        return _applicationDbContext.Roles.AnyAsync();
    }

    public Task<bool> RoleExistsAsync(string role)
    {
        role = role.ToUpperInvariant();
        return _applicationDbContext.Roles.Where(x => x.NormalizedName == role).AnyAsync();
    }

    public async Task<bool> RoleWithIdExistsAsync(string roleId)
    {
        return (await GetRoleWithIdAsync(roleId)) != null;
    }

    public Task<IdentityRole?> GetRoleAsync(string role)
    {
        role = role.ToUpperInvariant();
        return _applicationDbContext.Roles.Where(x => x.NormalizedName == role).FirstOrDefaultAsync();
    }

    public ValueTask<IdentityRole?> GetRoleWithIdAsync(string roleId)
    {
        return _applicationDbContext.Roles.FindAsync(roleId);
    }

    public async Task AddRoleAsync(IdentityRole role)
    {
        _applicationDbContext.Roles.Add(role);
        await _applicationDbContext.SaveChangesAsync();
    }
}
