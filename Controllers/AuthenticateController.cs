using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FutbalMng.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticateController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interaction;

        public AuthenticateController(IIdentityServerInteractionService interaction)
        {
            _interaction = interaction;
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginRequest request)
        {
            var context = await _interaction.GetAuthorizationContextAsync(request.ReturnUrl);
            var user = TestUsers.Users
                   .FirstOrDefault(usr => usr.Password == request.Password && usr.Username == request.Username);

            if (user != null && context != null)
            {
                await HttpContext.SignInAsync(user.SubjectId, user.Username);
                return new JsonResult(new { RedirectUrl = request.ReturnUrl, IsOk = true });
            }

            return Unauthorized();
        }

        public class LoginRequest{
            public string Username { get; set; }

            public string Password { get; set; }

            public string ReturnUrl { get; set; }
        }
    }
}