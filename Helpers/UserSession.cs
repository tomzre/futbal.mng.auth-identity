using System;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace futbal.mng.auth_identity.Helpers
{
    public class UserSession : DefaultUserSession
    {
        public UserSession(IHttpContextAccessor httpContextAccessor, IAuthenticationSchemeProvider schemes, IAuthenticationHandlerProvider handlers, IdentityServerOptions options, ISystemClock clock, ILogger<IUserSession> logger) : base(httpContextAccessor, schemes, handlers, options, clock, logger)
        {
        }

        public override async Task<ClaimsPrincipal> GetUserAsync()
        {
            await AuthenticateAsync();
            return base.Principal;
        }

        protected override async Task AuthenticateAsync()
        {
            if (Principal == null || Properties == null)
            {
                var scheme = await HttpContext.GetCookieAuthenticationSchemeAsync();

                var handler = await Handlers.GetHandlerAsync(HttpContext, scheme);
                if (handler == null)
                {
                    throw new InvalidOperationException($"No authentication handler is configured to authenticate for the scheme: {scheme}");
                }

                var result = await handler.AuthenticateAsync();
                if (result != null && result.Succeeded)
                {
                    Principal = result.Principal;
                    Properties = result.Properties;
                }
            }
        }
    }
    internal static class Extensions {
    internal static async Task<string> GetCookieAuthenticationSchemeAsync(this HttpContext context)
        {
            var options = context.RequestServices.GetRequiredService<IdentityServerOptions>();
            if (options.Authentication.CookieAuthenticationScheme != null)
            {
                return options.Authentication.CookieAuthenticationScheme;
            }

            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var scheme = await schemes.GetDefaultAuthenticateSchemeAsync();
            if (scheme == null)
            {
                throw new InvalidOperationException($"No DefaultAuthenticateScheme found or no CookieAuthenticationScheme configured on IdentityServerOptions.");
            }

            return scheme.Name;
        }
    }
}