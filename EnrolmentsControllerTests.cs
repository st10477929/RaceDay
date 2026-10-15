using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

/// <summary>Event enrolments: Participants enrol via a category and the link is recorded correctly.</summary>
public class EnrolmentsControllerTests
{
    private static EnrolmentsController As(RaceDayDbContext db, Api.Models.User user) =>
        new EnrolmentsController(db).WithSession(LoggedIn(user));

    [Fact]
    public async Task Enrol_ValidCategory_Returns201_AndRecordsParticipantCategoryAndEvent()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var participant = AddUser(db, RoleNames.Participant);
        var ev = AddEvent(db, organiser);
        var category = AddCategory(db, ev);

        var result = await As(db, participant).Enrol(category.CategoryId);

        Assert.Equal(201, Code(result));
        var saved = db.Enrolments.Single();
        Assert.Equal(participant.UserId, saved.ParticipantId);
        Assert.Equal(category.CategoryId, saved.CategoryId);
        Assert.Equal("Confirmed", saved.Status);

        var dto = Assert.IsType<EnrolmentDto>(Assert.IsType<CreatedResult>(result.Result).Value);
        Assert.Equal(ev.EventId, dto.EventId);
    }

    [Fact]
    public async Task Enrol_MissingCategory_Returns404()
    {
        var db = NewDb();
        var participant = AddUser(db, RoleNames.Participant);

        var result = await As(db, participant).Enrol(999);

        Assert.Equal(404, Code(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_AlreadyEnrolled_Returns409_AndDoesNotDuplicate()
    {
        var db = NewDb();
        var participant = AddUser(db, RoleNames.Participant);
        var category = AddCategory(db, AddEvent(db, AddUser(db, RoleNames.Organiser)));
        AddEnrolment(db, participant, category);

        var result = await As(db, participant).Enrol(category.CategoryId);

        Assert.Equal(409, Code(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Enrol_FullCategory_Returns409()
    {
        var db = NewDb();
        var category = AddCategory(db, AddEvent(db, AddUser(db, RoleNames.Organiser)), max: 1);
        AddEnrolment(db, AddUser(db, RoleNames.Participant), category);
        var latecomer = AddUser(db, RoleNames.Participant);

        var result = await As(db, latecomer).Enrol(category.CategoryId);

        Assert.Equal(409, Code(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var db = NewDb();
        var category = AddCategory(db, AddEvent(db, AddUser(db, RoleNames.Organiser)));
        var me = AddUser(db, RoleNames.Participant);
        var someoneElse = AddUser(db, RoleNames.Participant);
        AddEnrolment(db, me, category);
        AddEnrolment(db, someoneElse, category);

        var result = await As(db, me).GetMine();

        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
        Assert.Single(list);
        Assert.Equal(me.UserId, list[0].ParticipantId);
    }

    [Fact]
    public async Task GetForEvent_AsOwner_ReturnsAllEnrolmentsForThatEvent()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, organiser);
        var cat1 = AddCategory(db, ev, "5km");
        var cat2 = AddCategory(db, ev, "10km");
        AddEnrolment(db, AddUser(db, RoleNames.Participant), cat1);
        AddEnrolment(db, AddUser(db, RoleNames.Participant), cat2);

        var result = await As(db, organiser).GetForEvent(ev.EventId);

        var list = Assert.IsAssignableFrom<IEnumerable<EnrolmentDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(2, list.Count());
    }

    [Fact]
    public async Task GetForEvent_ByDifferentOrganiser_Returns403()
    {
        var db = NewDb();
        var ev = AddEvent(db, AddUser(db, RoleNames.Organiser));
        var other = AddUser(db, RoleNames.Organiser);

        var result = await As(db, other).GetForEvent(ev.EventId);

        Assert.Equal(403, Code(result));
    }

    [Fact]
    public async Task Cancel_OwnEnrolment_Returns204()
    {
        var db = NewDb();
        var participant = AddUser(db, RoleNames.Participant);
        var enrolment = AddEnrolment(db, participant, AddCategory(db, AddEvent(db, AddUser(db, RoleNames.Organiser))));

        var result = await As(db, participant).Cancel(enrolment.EnrolmentId);

        Assert.Equal(204, Code(result));
        Assert.Empty(db.Enrolments);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403_AndKeepsIt()
    {
        var db = NewDb();
        var owner = AddUser(db, RoleNames.Participant);
        var other = AddUser(db, RoleNames.Participant);
        var enrolment = AddEnrolment(db, owner, AddCategory(db, AddEvent(db, AddUser(db, RoleNames.Organiser))));

        var result = await As(db, other).Cancel(enrolment.EnrolmentId);

        Assert.Equal(403, Code(result));
        Assert.Single(db.Enrolments);
    }

    [Fact]
    public async Task Cancel_MissingEnrolment_Returns404()
    {
        var db = NewDb();
        var participant = AddUser(db, RoleNames.Participant);

        var result = await As(db, participant).Cancel(404);

        Assert.Equal(404, Code(result));
    }
}
