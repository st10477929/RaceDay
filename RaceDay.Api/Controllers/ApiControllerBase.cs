using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;

namespace RaceDay.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>ID of the logged-in user, read from the server-side session (0 if none).</summary>
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;

    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    protected ObjectResult Fail(int status, string message) => StatusCode(status, new ApiError(message));
}
