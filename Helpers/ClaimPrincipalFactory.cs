using FutbalMng.Auth.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FutbalMng.Auth.Helpers
{
    public sealed class ClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser, IdentityRole>
{
    public ClaimsPrincipalFactory(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor)
    {
    }
}
}