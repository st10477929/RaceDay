using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController(IApiClient api) : AppController
{
    // Public page: upcoming events with filtering (name/description, type, location, date range).
    public async Task<IActionResult> Index(string? search, string? type, string? location, DateTime? dateFrom, DateTime? dateTo)
    {
        var result = await api.GetAsync<List<EventDto>>("api/events?upcomingOnly=true");
        if (!result.Ok) ViewData["Error"] = result.Error;

        IEnumerable<EventDto> events = result.Data ?? new List<EventDto>();
        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(e => e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)
                                    || (e.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(location))
            events = events.Where(e => e.Location.Contains(location, StringComparison.OrdinalIgnoreCase));
        if (dateFrom is not null) events = events.Where(e => e.EventDate.Date >= dateFrom.Value.Date);
        if (dateTo is not null) events = events.Where(e => e.EventDate.Date <= dateTo.Value.Date);

        return View(new HomeViewModel
        {
            Events = events.ToList(), Search = search, Type = type, Location = location, DateFrom = dateFrom, DateTo = dateTo
        });
    }

    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult Error() => View();
}
