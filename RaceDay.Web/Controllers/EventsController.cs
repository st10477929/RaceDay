using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController(IApiClient api) : AppController
{
    // Public: full event information, categories and (once captured) results. Participants can enrol here.
    public async Task<IActionResult> Details(int id)
    {
        var ev = await api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Ok || ev.Data is null)
        {
            TempData["Error"] = ev.Error ?? "Event not found.";
            return RedirectToAction("Index", "Home");
        }

        var vm = new EventDetailsViewModel { Event = ev.Data };

        var cats = await api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        vm.Categories = cats.Data ?? new();

        var results = await api.GetAsync<List<ResultDto>>($"api/events/{id}/results");
        vm.Results = results.Data ?? new();

        if (Role == RoleNames.Participant)
        {
            var mine = await api.GetAsync<List<EnrolmentDto>>("api/enrolments/me");
            foreach (var e in mine.Data ?? new()) vm.EnrolledCategoryIds.Add(e.CategoryId);
        }

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, SessionAuthorize(RoleNames.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var ev = await api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev.Ok && ev.Data is not null && ev.Data.EventDate.Date < DateTime.Today)
        {
            TempData["Error"] = "This event has already taken place.";
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await api.PostAsync<EnrolmentDto>($"api/categories/{categoryId}/enrolments");
        if (result.Ok) TempData["Success"] = "You are enrolled! See it under My Events.";
        else TempData["Error"] = result.Error ?? "Enrolment failed.";

        return RedirectToAction(nameof(Details), new { id = eventId });
    }
}
