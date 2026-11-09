using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected int UserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
    protected string? Role => HttpContext.Session.GetString(SessionKeys.Role);

    /// <summary>First validation message, or a fallback, for showing in a banner after a redirect.</summary>
    protected string FirstError(string fallback) =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? fallback;
}
