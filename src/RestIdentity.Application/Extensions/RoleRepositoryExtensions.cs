using Microsoft.AspNetCore.Identity;

namespace RestIdentity.DataAccess.Repositories;

public static class RolesRepositoryExtensions
{
    public static Task AddRolesAsync(this IRoleRepository roleRepository, params IdentityRole[] roles)
    {
        return roleRepository.AddRolesAsync(roles);
    }
}
