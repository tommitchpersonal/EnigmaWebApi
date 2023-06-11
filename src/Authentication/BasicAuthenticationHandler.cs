using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using log4net;
public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private ICredentialRepository _credentialRepository;
    private ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, ICredentialRepository credentialRepository) : base(options, logger, encoder, clock)
    {
        _credentialRepository = credentialRepository;
    }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();

        if (authHeader != null && authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Basic ".Length).Trim();
            var credsAsEncodedString = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var credentials = credsAsEncodedString.Split(':');
            var user = credentials[0];

            if (await _credentialRepository.Authenticate(credentials[0], credentials[1]))
            {
                var claims = new[] {new Claim("name", user), new Claim(ClaimTypes.Role, "Admin")};
                var identity = new ClaimsIdentity(claims, "Basic");
                var claimsPrincipal = new ClaimsPrincipal(identity);
                _log.Info($"User {user} has been authenticated");
                return await Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, Scheme.Name)));
            }

            Response.StatusCode = 401;
            _log.Error("Credentials were incorrect");
            return await Task.FromResult(AuthenticateResult.Fail("Credentials were incorrect"));
        }

        Response.StatusCode = 401;
        _log.Error("Invalid authentication header");
        return await Task.FromResult(AuthenticateResult.Fail("Invalid Authorisation Header"));
    }


}
