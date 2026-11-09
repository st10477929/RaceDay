using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;
using static RaceDay.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
namespace RaceDay.Tests;

/// <summary>User registration and login: success and failure scenarios.</summary>
public class AuthControllerTests
{
    private static AuthController Make(Api.Data.RaceDayDbContext db, FakeSession? session = null) =>
        new AuthController(db, new PasswordService()).WithSession(session ?? new FakeSession());

    private static RegisterRequest ValidRequest(string role = "Participant", string email = "New@Example.com") => new()
    {
        FullName = "New User", Email = email, Password = "Password123!", Role = role
    };

    [Theory]
    [InlineData("Organiser")]
    [InlineData("participant")] // case-insensitive
    public async Task Register_ValidRole_Returns201_AndStoresHashedPassword(string role)
    {
        var db = NewDb();
        var result = await Make(db).Register(ValidRequest(role));

        Assert.Equal(201, Code(result));
        var saved = db.Users.Single();
        Assert.Equal("new@example.com", saved.Email);
        Assert.NotEqual("Password123!", saved.PasswordHash);                       // not stored in plain text
        Assert.True(new PasswordService().Verify("Password123!", saved.PasswordHash)); // but verifies correctly
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var db = NewDb();
        var controller = Make(db);
        await controller.Register(ValidRequest());

        var second = await controller.Register(ValidRequest(email: "new@example.com"));

        Assert.Equal(409, Code(second));
        Assert.Single(db.Users);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("")]
    public async Task Register_InvalidRole_Returns400_AndCreatesNoUser(string role)
    {
        var db = NewDb();
        var result = await Make(db).Register(ValidRequest(role));

        Assert.Equal(400, Code(result));
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200_AndStoresUserIdAndRoleInSession()
    {
        var db = NewDb();
        var user = AddUser(db, RoleNames.Organiser, "org@test.com", "Password123!");
        var session = new FakeSession();

        var result = await Make(db, session).Login(new LoginRequest { Email = "ORG@test.com", Password = "Password123!" });

        Assert.Equal(200, Code(result));
        Assert.Equal(user.UserId, session.GetInt32(SessionKeys.UserId));
        Assert.Equal(RoleNames.Organiser, session.GetString(SessionKeys.Role));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401_AndNoSession()
    {
        var db = NewDb();
        AddUser(db, RoleNames.Participant, "p@test.com", "Password123!");
        var session = new FakeSession();

        var result = await Make(db, session).Login(new LoginRequest { Email = "p@test.com", Password = "WrongPassword!" });

        Assert.Equal(401, Code(result));
        Assert.Null(session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var db = NewDb();
        var result = await Make(db).Login(new LoginRequest { Email = "nobody@test.com", Password = "Password123!" });

        Assert.Equal(401, Code(result));
    }

    [Fact]
    public async Task Login_NonBcryptStoredHash_Returns401_InsteadOfThrowing()
    {
        var db = NewDb();
        var user = AddUser(db, RoleNames.Participant, "legacy@test.com");
        user.PasswordHash = "HashedPassword123!"; // e.g. placeholder hash from the SQL seed script
        db.SaveChanges();

        var result = await Make(db).Login(new LoginRequest { Email = "legacy@test.com", Password = "HashedPassword123!" });

        Assert.Equal(401, Code(result));
    }

    [Fact]
    public void Logout_ClearsSession()
    {
        var db = NewDb();
        var user = AddUser(db, RoleNames.Participant);
        var session = LoggedIn(user);

        var result = Make(db, session).Logout();

        Assert.Equal(204, Code(result));
        Assert.Null(session.GetInt32(SessionKeys.UserId));
    }
}
