using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Auth;

/// <summary>Standard error body returned by the API.</summary>
public record ApiError(string Message);

public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    /// <summary>Returns the canonical role name, or null if the value is not a valid role.</summary>
    public static string? Normalise(string? role)
    {
        var r = role?.Trim();
        if (string.Equals(r, Organiser, StringComparison.OrdinalIgnoreCase)) return Organiser;
        if (string.Equals(r, Participant, StringComparison.OrdinalIgnoreCase)) return Participant;
        return null;
    }
}

public static class EventTypes
{
    private static readonly string[] Allowed = { "Run", "Walk", "Cycle" };

    public static string? Normalise(string? type) =>
        Allowed.FirstOrDefault(a => string.Equals(a, type?.Trim(), StringComparison.OrdinalIgnoreCase));
}

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

public static class SessionUserExtensions
{
    public static void SignIn(this ISession session, int userId, string role)
    {
        session.Clear();
        session.SetInt32(SessionKeys.UserId, userId);
        session.SetString(SessionKeys.Role, role);
    }
}

/// <summary>
/// Session-based authorisation filter.
/// No session -> 401 Unauthorized. Logged in but wrong role -> 403 Forbidden.
/// Use [RequireRole] for "any logged-in user" or [RequireRole("Organiser")] for specific roles.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    public string[] Roles { get; }

    public RequireRoleAttribute(params string[] roles) => Roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId is null || role is null)
        {
            context.Result = new ObjectResult(new ApiError("Authentication required. Please log in."))
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }

        if (Roles.Length > 0 && !Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new ApiError("You do not have permission to access this resource."))
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
