using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Organiser)]
public class OrganiserController(IApiClient api) : AppController
{
    // ---------- helpers ----------
    private async Task<List<EventDto>> MyEventsAsync()
    {
        var r = await api.GetAsync<List<EventDto>>("api/events");
        if (!r.Ok) ViewData["Error"] = r.Error;
        return (r.Data ?? new()).Where(e => e.OrganiserId == UserId).OrderBy(e => e.EventDate).ToList();
    }

    /// <summary>Loads an event only if it belongs to the logged-in Organiser (the API enforces this too).</summary>
    private async Task<EventDto?> OwnedEventAsync(int id)
    {
        var r = await api.GetAsync<EventDto>($"api/events/{id}");
        return r.Ok && r.Data is not null && r.Data.OrganiserId == UserId ? r.Data : null;
    }

    private IActionResult NotYours()
    {
        TempData["Error"] = "That event was not found, or it belongs to another organiser.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var vm = new DashboardViewModel();
        foreach (var ev in await MyEventsAsync())
        {
            var enrolments = await api.GetAsync<List<EnrolmentDto>>($"api/events/{ev.EventId}/enrolments");
            vm.Events.Add(new EventSummary { Event = ev, Enrolments = enrolments.Data?.Count ?? 0 });
        }
        return View(vm);
    }

    // ---------- events management ----------
    public async Task<IActionResult> Events() => View(await MyEventsAsync());

    [HttpGet]
    public IActionResult CreateEvent()
    {
        ViewData["Title"] = "Create event";
        ViewData["FormAction"] = nameof(CreateEvent);
        return View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        ViewData["Title"] = "Create event";
        ViewData["FormAction"] = nameof(CreateEvent);

        if (model.EventDate is not null && model.EventDate.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.EventDate), "The event date cannot be in the past.");
        if (!ModelState.IsValid) return View("EventForm", model);

        var created = await api.PostAsync<EventDto>("api/events", ToRequest(model));
        if (!created.Ok || created.Data is null)
        {
            ModelState.AddModelError("", created.Error ?? "Could not create the event.");
            return View("EventForm", model);
        }

        await UploadBannerIfAny(created.Data.EventId, model.BannerFile);
        TempData["Success"] ??= "Event created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await OwnedEventAsync(id);
        if (ev is null) return NotYours();

        ViewData["Title"] = "Edit event";
        ViewData["FormAction"] = nameof(EditEvent);
        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId, EventName = ev.EventName, Description = ev.Description, EventDate = ev.EventDate,
            Location = ev.Location, EventType = ev.EventType, CurrentBannerUrl = ev.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        ViewData["Title"] = "Edit event";
        ViewData["FormAction"] = nameof(EditEvent);
        if (!ModelState.IsValid) return View("EventForm", model);

        var updated = await api.PutAsync<EventDto>($"api/events/{model.EventId}", ToRequest(model));
        if (!updated.Ok)
        {
            ModelState.AddModelError("", updated.Error ?? "Could not update the event.");
            return View("EventForm", model);
        }

        await UploadBannerIfAny(model.EventId, model.BannerFile);
        TempData["Success"] ??= "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var r = await api.DeleteAsync($"api/events/{id}");
        if (r.Ok) TempData["Success"] = "Event deleted.";
        else TempData["Error"] = r.Error ?? "Could not delete the event.";
        return RedirectToAction(nameof(Events));
    }

    private static object ToRequest(EventFormViewModel m) => new
    {
        m.EventName, m.Description, EventDate = m.EventDate!.Value.Date, m.Location, m.EventType
    };

    private async Task UploadBannerIfAny(int eventId, IFormFile? file)
    {
        if (file is null || file.Length == 0) return;
        var up = await api.UploadAsync<EventDto>($"api/events/{eventId}/banner", file);
        if (up.Ok) TempData["Success"] = "Event saved with its banner image.";
        else TempData["Error"] = "The event was saved, but the banner failed: " + (up.Error ?? "upload error.");
    }

    // ---------- categories ----------
    [HttpGet]
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev is null) return NotYours();
        return View(await BuildCategoriesAsync(ev, new CategoryFormViewModel { EventId = eventId }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel form)
    {
        var ev = await OwnedEventAsync(form.EventId);
        if (ev is null) return NotYours();

        if (!ModelState.IsValid) return View("Categories", await BuildCategoriesAsync(ev, form));

        var r = await api.PostAsync<CategoryDto>($"api/events/{form.EventId}/categories",
            new { form.CategoryName, form.Distance, form.MaxParticipants, form.EntryFee });
        if (!r.Ok)
        {
            ModelState.AddModelError("", r.Error ?? "Could not add the category.");
            return View("Categories", await BuildCategoriesAsync(ev, form));
        }

        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories), new { eventId = form.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var r = await api.DeleteAsync($"api/categories/{id}");
        if (r.Ok) TempData["Success"] = "Category deleted.";
        else TempData["Error"] = r.Error ?? "Could not delete the category.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private async Task<CategoriesViewModel> BuildCategoriesAsync(EventDto ev, CategoryFormViewModel form)
    {
        var cats = await api.GetAsync<List<CategoryDto>>($"api/events/{ev.EventId}/categories");
        return new CategoriesViewModel { Event = ev, Categories = cats.Data ?? new(), Form = form };
    }

    // ---------- enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev is null) return NotYours();

        var r = await api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        if (!r.Ok) ViewData["Error"] = r.Error;
        return View(new EnrolmentsViewModel { Event = ev, Enrolments = r.Data ?? new() });
    }

    // ---------- results entry ----------
    [HttpGet]
    public async Task<IActionResult> Results(int eventId)
    {
        var ev = await OwnedEventAsync(eventId);
        if (ev is null) return NotYours();

        var enrolments = await api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        var results = await api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");

        return View(new OrganiserResultsViewModel
        {
            Event = ev,
            Enrolments = enrolments.Data ?? new(),
            ResultsByEnrolment = (results.Data ?? new()).ToDictionary(r => r.EnrolmentId)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model)
    {
        var ev = await OwnedEventAsync(model.EventId);
        if (ev is null) return NotYours();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError("Please enter the finish time as hh:mm:ss and a position of 1 or higher.");
            return RedirectToAction(nameof(Results), new { eventId = model.EventId });
        }

        var body = new { finishTime = model.FinishTime, position = model.Position };
        var r = model.ResultId > 0
            ? await api.PutAsync<ResultDto>($"api/results/{model.ResultId}", body)
            : await api.PostAsync<ResultDto>($"api/enrolments/{model.EnrolmentId}/results", body);

        if (r.Ok) TempData["Success"] = "Result saved and published.";
        else TempData["Error"] = r.Error ?? "Could not save the result.";
        return RedirectToAction(nameof(Results), new { eventId = model.EventId });
    }
}
