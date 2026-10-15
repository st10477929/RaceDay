using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Auth;
using RaceDay.Api.Controllers;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using static RaceDay.Tests.TestHelpers;

namespace RaceDay.Tests;

/// <summary>Results capture/viewing and category management.</summary>
public class ResultsAndCategoriesTests
{
    private static ResultsController Results(RaceDayDbContext db, Api.Models.User user) =>
        new ResultsController(db).WithSession(LoggedIn(user));

    private static CategoriesController Categories(RaceDayDbContext db, Api.Models.User user) =>
        new CategoriesController(db).WithSession(LoggedIn(user));

    private static ResultRequest Req(int position = 1) =>
        new() { FinishTime = TimeSpan.FromMinutes(48.5), Position = position };

    // ---------- Results ----------
    [Fact]
    public async Task Capture_AsOwningOrganiser_Returns201_AndLinksResultToEnrolment()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var enrolment = AddEnrolment(db, AddUser(db, RoleNames.Participant), AddCategory(db, AddEvent(db, organiser)));

        var result = await Results(db, organiser).Capture(enrolment.EnrolmentId, Req(3));

        Assert.Equal(201, Code(result));
        var saved = db.Results.Single();
        Assert.Equal(enrolment.EnrolmentId, saved.EnrolmentId);
        Assert.Equal(3, saved.Position);
        Assert.Equal(organiser.UserId, saved.CapturedByOrganiserId);
    }

    [Fact]
    public async Task Capture_ForEnrolmentThatAlreadyHasResult_Returns409()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var enrolment = AddEnrolment(db, AddUser(db, RoleNames.Participant), AddCategory(db, AddEvent(db, organiser)));
        var controller = Results(db, organiser);
        await controller.Capture(enrolment.EnrolmentId, Req());

        var second = await controller.Capture(enrolment.EnrolmentId, Req(2));

        Assert.Equal(409, Code(second));
        Assert.Single(db.Results);
    }

    [Fact]
    public async Task Capture_ByOrganiserWhoDoesNotOwnTheEvent_Returns403()
    {
        var db = NewDb();
        var owner = AddUser(db, RoleNames.Organiser);
        var other = AddUser(db, RoleNames.Organiser);
        var enrolment = AddEnrolment(db, AddUser(db, RoleNames.Participant), AddCategory(db, AddEvent(db, owner)));

        var result = await Results(db, other).Capture(enrolment.EnrolmentId, Req());

        Assert.Equal(403, Code(result));
        Assert.Empty(db.Results);
    }

    [Fact]
    public async Task Capture_MissingEnrolment_Returns404()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);

        var result = await Results(db, organiser).Capture(999, Req());

        Assert.Equal(404, Code(result));
    }

    [Fact]
    public async Task Update_AsOwningOrganiser_ChangesPosition()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var enrolment = AddEnrolment(db, AddUser(db, RoleNames.Participant), AddCategory(db, AddEvent(db, organiser)));
        var controller = Results(db, organiser);
        await controller.Capture(enrolment.EnrolmentId, Req(5));

        var result = await controller.Update(db.Results.Single().ResultId, Req(2));

        Assert.Equal(200, Code(result));
        Assert.Equal(2, db.Results.Single().Position);
    }

    [Fact]
    public async Task GetMine_ReturnsOnlyTheParticipantsOwnResults()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var category = AddCategory(db, AddEvent(db, organiser));
        var me = AddUser(db, RoleNames.Participant);
        var other = AddUser(db, RoleNames.Participant);
        var myEnrolment = AddEnrolment(db, me, category);
        var otherEnrolment = AddEnrolment(db, other, category);
        var organiserController = Results(db, organiser);
        await organiserController.Capture(myEnrolment.EnrolmentId, Req(1));
        await organiserController.Capture(otherEnrolment.EnrolmentId, Req(2));

        var result = await Results(db, me).GetMine();

        var list = Assert.IsAssignableFrom<IEnumerable<ResultDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
        Assert.Single(list);
        Assert.Equal(1, list[0].Position);
    }

    [Fact]
    public async Task GetForEvent_ReturnsLeaderboardOrderedByPosition()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, organiser);
        var category = AddCategory(db, ev);
        var e1 = AddEnrolment(db, AddUser(db, RoleNames.Participant), category);
        var e2 = AddEnrolment(db, AddUser(db, RoleNames.Participant), category);
        var controller = Results(db, organiser);
        await controller.Capture(e1.EnrolmentId, Req(2));
        await controller.Capture(e2.EnrolmentId, Req(1));

        var result = await new ResultsController(db).WithSession(new FakeSession()).GetForEvent(ev.EventId);

        var list = Assert.IsAssignableFrom<IEnumerable<ResultDto>>(Assert.IsType<OkObjectResult>(result.Result).Value).ToList();
        Assert.Equal(new[] { 1, 2 }, list.Select(r => r.Position));
    }

    // ---------- Categories ----------
    [Fact]
    public async Task CreateCategory_AsOwningOrganiser_Returns201()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var ev = AddEvent(db, organiser);

        var result = await Categories(db, organiser).Create(ev.EventId,
            new CategoryRequest { CategoryName = "21km", Distance = 21.1m, MaxParticipants = 200, EntryFee = 250 });

        Assert.Equal(201, Code(result));
        Assert.Equal("21km", db.Categories.Single().CategoryName);
    }

    [Fact]
    public async Task CreateCategory_ByOrganiserWhoDoesNotOwnTheEvent_Returns403()
    {
        var db = NewDb();
        var ev = AddEvent(db, AddUser(db, RoleNames.Organiser));
        var other = AddUser(db, RoleNames.Organiser);

        var result = await Categories(db, other).Create(ev.EventId,
            new CategoryRequest { CategoryName = "5km", Distance = 5, MaxParticipants = 50, EntryFee = 0 });

        Assert.Equal(403, Code(result));
        Assert.Empty(db.Categories);
    }

    [Fact]
    public async Task GetCategories_ForMissingEvent_Returns404()
    {
        var db = NewDb();
        var result = await new CategoriesController(db).WithSession(new FakeSession()).GetForEvent(999);

        Assert.Equal(404, Code(result));
    }

    [Fact]
    public async Task GetCategories_IsPublic_AndReturnsEventCategories()
    {
        var db = NewDb();
        var ev = AddEvent(db, AddUser(db, RoleNames.Organiser));
        AddCategory(db, ev, "10km");

        var result = await new CategoriesController(db).WithSession(new FakeSession()).GetForEvent(ev.EventId);

        var list = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var db = NewDb();
        var organiser = AddUser(db, RoleNames.Organiser);
        var category = AddCategory(db, AddEvent(db, organiser));
        AddEnrolment(db, AddUser(db, RoleNames.Participant), category);

        var result = await Categories(db, organiser).Delete(category.CategoryId);

        Assert.Equal(409, Code(result));
        Assert.Single(db.Categories);
    }
}
