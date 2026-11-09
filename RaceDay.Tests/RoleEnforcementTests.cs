using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;

namespace RaceDay.Tests;

/// <summary>
/// Unauthenticated requests are rejected (401) and the wrong role is rejected (403).
/// Also verifies that the role-protected endpoints actually carry the correct [RequireRole] attribute.
/// </summary>
public class RoleEnforcementTests
{
    private static AuthorizationFilterContext Context(FakeSession session)
    {
        var http = new DefaultHttpContext { Session = session };
        var actionContext = new ActionContext(http, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static FakeSession Session(int? userId, string? role)
    {
        var s = new FakeSession();
        if (userId is not null) s.SetInt32(SessionKeys.UserId, userId.Value);
        if (role is not null) s.SetString(SessionKeys.Role, role);
        return s;
    }

    private static int? Status(AuthorizationFilterContext ctx) => (ctx.Result as ObjectResult)?.StatusCode;

    [Fact]
    public void NoSession_IsRejectedWith401()
    {
        var ctx = Context(Session(null, null));
        new RequireRoleAttribute(RoleNames.Organiser).OnAuthorization(ctx);
        Assert.Equal(401, Status(ctx));
    }

    [Fact]
    public void NoSession_OnAnyLoggedInEndpoint_IsRejectedWith401()
    {
        var ctx = Context(Session(null, null));
        new RequireRoleAttribute().OnAuthorization(ctx);
        Assert.Equal(401, Status(ctx));
    }

    [Fact]
    public void Participant_OnOrganiserEndpoint_IsRejectedWith403()
    {
        var ctx = Context(Session(2, RoleNames.Participant));
        new RequireRoleAttribute(RoleNames.Organiser).OnAuthorization(ctx);
        Assert.Equal(403, Status(ctx));
    }

    [Fact]
    public void Organiser_OnParticipantEndpoint_IsRejectedWith403()
    {
        var ctx = Context(Session(1, RoleNames.Organiser));
        new RequireRoleAttribute(RoleNames.Participant).OnAuthorization(ctx);
        Assert.Equal(403, Status(ctx));
    }

    [Fact]
    public void Organiser_OnOrganiserEndpoint_IsAllowed()
    {
        var ctx = Context(Session(1, RoleNames.Organiser));
        new RequireRoleAttribute(RoleNames.Organiser).OnAuthorization(ctx);
        Assert.Null(ctx.Result);
    }

    [Fact]
    public void Participant_OnParticipantEndpoint_IsAllowed()
    {
        var ctx = Context(Session(2, RoleNames.Participant));
        new RequireRoleAttribute(RoleNames.Participant).OnAuthorization(ctx);
        Assert.Null(ctx.Result);
    }

    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public void AnyLoggedInUser_IsAllowedOnAnyLoggedInEndpoint(string role)
    {
        var ctx = Context(Session(5, role));
        new RequireRoleAttribute().OnAuthorization(ctx);
        Assert.Null(ctx.Result);
    }

    // ---- the endpoints themselves are protected with the right role ----
    [Theory]
    [InlineData(typeof(EventsController), nameof(EventsController.Create), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Update), RoleNames.Organiser)]
    [InlineData(typeof(EventsController), nameof(EventsController.Delete), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Create), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Update), RoleNames.Organiser)]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Delete), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetForEvent), RoleNames.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.Capture), RoleNames.Organiser)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.Update), RoleNames.Organiser)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Enrol), RoleNames.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.GetMine), RoleNames.Participant)]
    [InlineData(typeof(EnrolmentsController), nameof(EnrolmentsController.Cancel), RoleNames.Participant)]
    [InlineData(typeof(ResultsController), nameof(ResultsController.GetMine), RoleNames.Participant)]
    public void Endpoint_IsProtectedByCorrectRole(Type controller, string action, string expectedRole)
    {
        var attribute = controller.GetMethod(action)!.GetCustomAttribute<RequireRoleAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(new[] { expectedRole }, attribute!.Roles);
    }

    [Fact]
    public void UsersController_RequiresLogin_ForAllEndpoints()
    {
        var attribute = typeof(UsersController).GetCustomAttribute<RequireRoleAttribute>();
        Assert.NotNull(attribute);
        Assert.Empty(attribute!.Roles); // any authenticated role
    }
}
