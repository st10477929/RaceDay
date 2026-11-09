using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(RoleNames.Participant)]
public class ParticipantController(IApiClient api) : AppController
{
    // Personal view of every event the participant has enrolled for, with status.
    public async Task<IActionResult> MyEvents()
    {
        var result = await api.GetAsync<List<EnrolmentDto>>("api/enrolments/me");
        if (!result.Ok) ViewData["Error"] = result.Error;

        // Event dates for display / deciding whether cancelling is still possible.
        var events = await api.GetAsync<List<EventDto>>("api/events");
        ViewData["EventDates"] = (events.Data ?? new()).ToDictionary(e => e.EventId, e => e.EventDate);

        return View((result.Data ?? new()).OrderBy(e => e.EventName).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await api.DeleteAsync($"api/enrolments/{id}");
        if (result.Ok) TempData["Success"] = "Your enrolment has been cancelled.";
        else TempData["Error"] = result.Error ?? "Could not cancel the enrolment.";
        return RedirectToAction(nameof(MyEvents));
    }

    // Personal race history: event, date, category, finish time and finishing position (e.g. 47th out of 312 finishers).
    public async Task<IActionResult> MyResults()
    {
        var mine = await api.GetAsync<List<ResultDto>>("api/results/me");
        if (!mine.Ok) ViewData["Error"] = mine.Error;
        var results = mine.Data ?? new();

        var events = await api.GetAsync<List<EventDto>>("api/events");
        var dates = (events.Data ?? new()).ToDictionary(e => e.EventId, e => e.EventDate);

        // Count finishers per (event, category) from each event's public leaderboard.
        var finishers = new Dictionary<(int, string), int>();
        foreach (var eventId in results.Select(r => r.EventId).Distinct())
        {
            var board = await api.GetAsync<List<ResultDto>>($"api/events/{eventId}/results");
            foreach (var g in (board.Data ?? new()).GroupBy(r => r.CategoryName))
                finishers[(eventId, g.Key)] = g.Count();
        }

        var rows = results.Select(r => new MyResultRow
        {
            Result = r,
            EventDate = dates.TryGetValue(r.EventId, out var d) ? d : null,
            Finishers = finishers.TryGetValue((r.EventId, r.CategoryName), out var n) ? n : 0
        }).OrderByDescending(r => r.EventDate).ToList();

        return View(rows);
    }
}
