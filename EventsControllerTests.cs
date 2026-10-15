using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

/// <summary>Event management: Organisers can create, update and delete their own events; everyone can view.</summary>
public class EventsControllerTests
{
    private static EventsController As(RaceDayDbContext db, Api.Models.User user) =>
        new EventsController(db).WithSession(LoggedIn(user));

    private static EventRequest Request(string type = "Run", string name = "Spring 10K") => new()
    {
        EventName = name, Description = "A fun race", EventDate = DateTime.Today.AddDays(14),
        Location = "Pretoria", EventType = type
    };

    [Fact]
    public async Task Create_AsOrganiser_Returns201_AndOwnsTheEvent()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);

        var result = await As(db, organiser).Create(Request("cycle"));

        Assert.Equal(201, Code(result));
        var saved = db.Events.Single();
        Assert.Equal(organiser.UserId, saved.OrganiserId);
        Assert.Equal("Cycle", saved.EventType); // normalised
    }

    [Fact]
    public async Task Create_InvalidEventType_Returns400()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);

        var result = await As(db, organiser).Create(Request("Swim"));

        Assert.Equal(400, Code(result));
        Assert.Empty(db.Events);
    }

    [Fact]
    public async Task Update_AsOwner_Returns200_AndPersistsChanges()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, organiser);

        var result = await As(db, organiser).Update(ev.EventId, Request("Walk", "Renamed Walk"));

        Assert.Equal(200, Code(result));
        Assert.Equal("Renamed Walk", db.Events.Single().EventName);
        Assert.Equal("Walk", db.Events.Single().EventType);
    }

    [Fact]
    public async Task Update_ByDifferentOrganiser_Returns403_AndDoesNotChangeEvent()
    {
        var db = NewDb();
        var owner = AddUser(db, RoleNames.Organiser);
        var other = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, owner, "Original");

        var result = await As(db, other).Update(ev.EventId, Request(name: "Hijacked"));

        Assert.Equal(403, Code(result));
        Assert.Equal("Original", db.Events.Single().EventName);
    }

    [Fact]
    public async Task Update_MissingEvent_Returns404()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);

        var result = await As(db, organiser).Update(999, Request());

        Assert.Equal(404, Code(result));
    }

    [Fact]
    public async Task Delete_AsOwner_Returns204_AndRemovesEventAndCategories()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, organiser);
        AddCategory(db, ev);

        var result = await As(db, organiser).Delete(ev.EventId);

        Assert.Equal(204, Code(result));
        Assert.Empty(db.Events);
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task Delete_ByDifferentOrganiser_Returns403_AndKeepsEvent()
    {
        var db = NewDb();
        var owner = AddUser(db, RoleNames.Organiser);
        var other = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, owner);

        var result = await As(db, other).Delete(ev.EventId);

        Assert.Equal(403, Code(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task Delete_EventWithEnrolments_Returns409()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var participant = AddUser(db, RoleNames.Participant);
        var ev = AddEvent(db, organiser);
        AddEnrolment(db, participant, AddCategory(db, ev));

        var result = await As(db, organiser).Delete(ev.EventId);

        Assert.Equal(409, Code(result));
        Assert.Single(db.Events);
    }

    [Fact]
    public async Task GetAll_ReturnsAllEvents_OrderedByDate_WithoutNeedingASession()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var later = AddEvent(db, organiser, "Later");
        later.EventDate = DateTime.Today.AddDays(60);
        AddEvent(db, organiser, "Sooner");
        db.SaveChanges();

        var result = await new EventsController(db).WithSession(new FakeSession()).GetAll();

        var events = Assert.IsAssignableFrom<IEnumerable<EventDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
        Assert.Equal(new[] { "Sooner", "Later" }, events.Select(e => e.EventName));
    }

    [Fact]
    public async Task GetById_MissingEvent_Returns404()
    {
        var db = NewDb();
        var result = await new EventsController(db).WithSession(new FakeSession()).GetById(123);

        Assert.Equal(404, Code(result));
    }

    [Fact]
    public async Task GetById_ExistingEvent_Returns200()
    {
        var db = NewDb();
        var ev = AddEvent(db, AddUser(db, RoleNames.Organiser));

        var result = await new EventsController(db).WithSession(new FakeSession()).GetById(ev.EventId);

        Assert.Equal(200, Code(result));
    }
}
