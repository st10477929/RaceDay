using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

/// <summary>User profile: both roles can view and update their own profile only.</summary>
public class UsersControllerTests
{
    [Theory]
    [InlineData(RoleNames.Organiser)]
    [InlineData(RoleNames.Participant)]
    public async Task GetMe_ReturnsTheLoggedInUsersProfile(string role)
    {
        var db = NewDb();
        var user = AddUser(db, role, "me@test.com");
        AddUser(db, role, "someone-else@test.com");

        var result = await new UsersController(db).WithSession(LoggedIn(user)).GetMe();

        var dto = Assert.IsType<UserDto>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("me@test.com", dto.Email);
        Assert.Equal(role, dto.Role);
    }

    [Fact]
    public async Task UpdateMe_ChangesOnlyTheLoggedInUsersDetails()
    {
        var db = NewDb();
        var me = AddUser(db, RoleNames.Participant, "me@test.com");
        var other = AddUser(db, RoleNames.Participant, "other@test.com");
        var otherName = other.FullName;

        var result = await new UsersController(db).WithSession(LoggedIn(me))
            .UpdateMe(new UpdateProfileRequest { FullName = "  Updated Name ", PhoneNumber = "0821234567" });

        Assert.Equal(200, Code(result));
        Assert.Equal("Updated Name", db.Users.Single(u => u.Email == "me@test.com").FullName);
        Assert.Equal("0821234567", db.Users.Single(u => u.Email == "me@test.com").PhoneNumber);
        Assert.Equal(otherName, db.Users.Single(u => u.Email == "other@test.com").FullName);
    }
}
