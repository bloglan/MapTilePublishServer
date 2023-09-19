using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace TileMapService
{
    class SimpleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public SimpleAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return AuthenticateResult.NoResult();
        }
    }
}
