using Edelstein.Server.Configuration.Metrics;

using Microsoft.Extensions.Options;

using System.Net.Http.Headers;
using System.Text;

namespace Edelstein.Server.Authorization;

public class MetricsAuthorizationMiddleware : IMiddleware
{
    private readonly IOptions<MetricsOptions> _metricsOptions;

    public MetricsAuthorizationMiddleware(IOptions<MetricsOptions> metricsOptions) =>
        _metricsOptions = metricsOptions;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string? authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (authHeader == null)
        {
            context.Response.Headers.WWWAuthenticate = "Basic";
            context.Response.StatusCode = 401;
            return;
        }

        AuthenticationHeaderValue authHeaderVal = AuthenticationHeaderValue.Parse(authHeader);
        if (!authHeaderVal.Scheme.Equals("basic", StringComparison.OrdinalIgnoreCase) || authHeaderVal.Parameter is null)
        {
            context.Response.Headers.WWWAuthenticate = "Basic";
            context.Response.StatusCode = 401;
            return;
        }

        string[] credentialItems = Encoding.UTF8.GetString(Convert.FromBase64String(authHeaderVal.Parameter)).Split(":", 2);
        string username = credentialItems[0];
        string password = credentialItems[1];

        bool authorized = username == _metricsOptions.Value.Authentication.Username &&
            password == _metricsOptions.Value.Authentication.Password;

        if (!authorized)
        {
            context.Response.StatusCode = 403;
            return;
        }

        await next(context);
    }
}
