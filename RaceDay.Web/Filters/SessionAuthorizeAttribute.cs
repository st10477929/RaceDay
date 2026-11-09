using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects MVC pages using the MVC session.
/// Not logged in -> redirect to Login. Wrong role -> redirect to Access Denied.
/// Usage: [SessionAuthorize] (any user) or [SessionAuthorize(RoleNames.Organiser)].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SessionAuthorizeAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || role is null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (roles.Length > 0 && !roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
