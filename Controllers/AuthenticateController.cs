using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FutbalMng.Auth.Data;
using IdentityServer4.Events;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FutbalMng.Auth.Controllers {
    [Route ("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthenticateController : ControllerBase {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEventService _events;

        public AuthenticateController (IIdentityServerInteractionService interaction,
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            IEventService events) {
            _interaction = interaction;
            _signInManager = signInManager;
            _userManager = userManager;
            _events = events;
        }

        [HttpPost]
        public async Task<IActionResult> Login (LoginRequest request) {
            if (!ModelState.IsValid) {
                return BadRequest (ModelState);
            }
            var context = await _interaction.GetAuthorizationContextAsync (request.ReturnUrl);
            var result = await _signInManager.PasswordSignInAsync (request.Username, request.Password, false, false);
            if (result.Succeeded) {
                var user = await _userManager.FindByNameAsync (request.Username);
                AuthenticationProperties props = new AuthenticationProperties {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add (AccountOptions.RememberMeLoginDuration)
                };

                await HttpContext.SignInAsync (user.Id, user.UserName, props);
                await _events.RaiseAsync (new UserLoginSuccessEvent (user.UserName, user.Id, user.Name));
                if (context != null) {

                    return new JsonResult (new { RedirectUrl = request.ReturnUrl, IsOk = true });
                }
            }

            return Unauthorized ();
        }

        [HttpGet]
        [Route ("Logout")]
        public async Task<IActionResult> Logout (string logoutId) {
            var context = await _interaction.GetLogoutContextAsync (logoutId);
            bool showSignoutPrompt = true;

            if (context?.ShowSignoutPrompt == false) {
                // it's safe to automatically sign-out
                showSignoutPrompt = false;
            }

            if (User?.Identity.IsAuthenticated == true) {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();
            }

            // no external signout supported for now (see \Quickstart\Account\AccountController.cs TriggerExternalSignout)
            return Ok (new {
                showSignoutPrompt,
                ClientName = string.IsNullOrEmpty (context?.ClientName) ? context?.ClientId : context?.ClientName,
                context?.PostLogoutRedirectUri,
                context?.SignOutIFrameUrl,
                logoutId
            });
        }

        public class LoginRequest {
            public string Username { get; set; }

            public string Password { get; set; }

            [Required]
            public string ReturnUrl { get; set; }
        }
    }
}