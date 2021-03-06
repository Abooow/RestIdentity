using Microsoft.AspNetCore.Identity;

namespace RestIdentity.DataAccess.Repositories;

public interface IRolesRepository
{
    Task<bool> AnyRolesExistsAsync();
    Task<bool> RoleExistsAsync(string role);
    Task<bool> RoleWithIdExistsAsync(string roleId);
    Task<IdentityRole?> GetRoleAsync(string role);
    ValueTask<IdentityRole?> GetRoleWithIdAsync(string roleId);
    Task AddRoleAsync(IdentityRole role);
    Task AddRolesAsync(IEnumerable<IdentityRole> roles);
}
