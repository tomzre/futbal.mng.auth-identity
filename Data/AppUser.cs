using Microsoft.AspNetCore.Identity;

namespace FutbalMng.Auth.Data
{
    public class AppUser : IdentityUser
    {
        public string Name { get; set; }
    }
}