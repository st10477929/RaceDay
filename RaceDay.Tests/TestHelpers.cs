using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>In-memory ISession so controllers can be tested without a web server.</summary>
public class FakeSession : ISession
{
    private readonly Dictionary<string, byte[]> _store = new();
    public bool IsAvailable => true;
    public string Id => "test-session";
    public IEnumerable<string> Keys => _store.Keys;
    public void Clear() => _store.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _store.Remove(key);
    public void Set(string key, byte[] value) => _store[key] = value;
    public bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out byte[]? value) => _store.TryGetValue(key, out value);
}

public static class TestHelpers
{
    public static RaceDayDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<RaceDayDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new RaceDayDbContext(options);
        db.Database.EnsureCreated(); // applies the seeded roles
        return db;
    }

    public static FakeSession LoggedIn(User user)
    {
        var session = new FakeSession();
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role.RoleName);
        return session;
    }

    public static T WithSession<T>(this T controller, ISession session) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
        };
        return controller;
    }

    public static int? Code(IActionResult? result) => (result as IStatusCodeActionResult)?.StatusCode;
    public static int? Code<T>(ActionResult<T> result) => Code(result.Result);

    // ---------- data builders ----------
    public static User AddUser(RaceDayDbContext db, string role, string? email = null, string password = "Password123!")
    {
        var roleEntity = db.Roles.Single(r => r.RoleName == role);
        var user = new User
        {
            RoleId = roleEntity.RoleId,
            Role = roleEntity,
            FullName = $"{role} User",
            Email = email ?? $"{Guid.NewGuid():N}@test.com",
            PasswordHash = new PasswordService().Hash(password)
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Event AddEvent(RaceDayDbContext db, User organiser, string name = "Test 10K")
    {
        var ev = new Event
        {
            OrganiserId = organiser.UserId,
            Organiser = organiser,
            EventName = name,
            EventDate = DateTime.Today.AddDays(30),
            Location = "Johannesburg",
            EventType = "Run"
        };
        db.Events.Add(ev);
        db.SaveChanges();
        return ev;
    }

    public static Category AddCategory(RaceDayDbContext db, Event ev, string name = "10km", int max = 100)
    {
        var category = new Category { EventId = ev.EventId, Event = ev, CategoryName = name, Distance = 10, MaxParticipants = max, EntryFee = 150 };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }

    public static Enrolment AddEnrolment(RaceDayDbContext db, User participant, Category category)
    {
        var enrolment = new Enrolment
        {
            ParticipantId = participant.UserId, Participant = participant,
            CategoryId = category.CategoryId, Category = category
        };
        db.Enrolments.Add(enrolment);
        db.SaveChanges();
        return enrolment;
    }
}
