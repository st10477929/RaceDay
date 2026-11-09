using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>If the API says our session has ended, clear the MVC session and send the user to the Login page.</summary>
public sealed class SessionExpiredFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context) { }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (!context.HttpContext.Items.ContainsKey(SessionKeys.ApiExpired)) return;
        context.HttpContext.Session.Clear();
        context.Result = new RedirectToActionResult("Login", "Account", new { expired = true });
    }
}
