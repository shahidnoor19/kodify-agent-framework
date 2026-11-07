using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SupportCopilot.Application.Security;

namespace SupportCopilot.Web.Security;

/// <summary>
/// Demo sign-in for the course: two staff users and one customer, no passwords.
/// In a real company this is Entra ID (Microsoft.Identity.Web); nothing below the web layer changes,
/// because the agents and services only see a <see cref="CallerContext"/>.
/// </summary>
public static class DemoUsers
{
    public static readonly IReadOnlyList<CallerContext> All =
    [
        new("alice", Roles.Agent),
        new("sam", Roles.Supervisor),
        new("ayesha", Roles.Customer, "ayesha@contoso.com"),
    ];

    public static ClaimsPrincipal ToPrincipal(CallerContext user)
    {
        List<Claim> claims = [new(ClaimTypes.Name, user.UserName), new(ClaimTypes.Role, user.Role)];
        if (user.CustomerEmail is not null) claims.Add(new(ClaimTypes.Email, user.CustomerEmail));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}

public static class WebSecurity
{
    public static void AddWebSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/Account/Login";
                // Pages redirect to the login page; API calls (fetch, SSE) get a clean 401 / 403 instead.
                o.Events.OnRedirectToLogin = ctx => IsApiCall(ctx.Request) ? Status(ctx, 401) : Redirect(ctx);
                o.Events.OnRedirectToAccessDenied = ctx => Status(ctx, 403);
            });
        builder.Services.AddAuthorization();
    }

    private static bool IsApiCall(HttpRequest request) => !HttpMethods.IsGet(request.Method);

    private static Task Status(RedirectContext<CookieAuthenticationOptions> ctx, int status)
    {
        ctx.Response.StatusCode = status;
        return Task.CompletedTask;
    }

    private static Task Redirect(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }

    /// <summary>Puts the signed-in user into the caller context for everything this request does.</summary>
    public static IApplicationBuilder UseCallerContext(this IApplicationBuilder app) => app.Use(async (ctx, next) =>
    {
        var user = ctx.User;
        var caller = user.Identity?.IsAuthenticated == true
            ? new CallerContext(user.Identity.Name!, user.FindFirstValue(ClaimTypes.Role) ?? "", user.FindFirstValue(ClaimTypes.Email))
            : CallerContext.Anonymous;
        using var _ = ctx.RequestServices.GetRequiredService<ICallerContextAccessor>().Begin(caller);
        await next();
    });
}
