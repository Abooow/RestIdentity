using Microsoft.AspNetCore.Identity;

namespace RestIdentity.DataAccess.Repositories;

public static class RolesRepositoryExtensions
{
    public static Task AddRolesAsync(this IRolesRepository rolesRepository, params IdentityRole[] roles)
    {
        return rolesRepository.AddRolesAsync(roles);
    }
}
