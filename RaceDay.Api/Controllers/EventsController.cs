using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Lists events, ordered by date. Public.</summary>
    /// <param name="upcomingOnly">When true, only events on or after today are returned.</param>
    /// <response code="200">Array of events.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAll([FromQuery] bool upcomingOnly = false)
    {
        var query = db.Events.Include(e => e.Organiser).AsQueryable();
        if (upcomingOnly) query = query.Where(e => e.EventDate >= DateTime.Today);

        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(EventDto.From).ToList());
    }

    /// <summary>Returns a single event. Public.</summary>
    /// <response code="200">The event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(int id)
    {
        var ev = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? Fail(404, "Event not found.") : Ok(EventDto.From(ev));
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>EventType must be Run, Walk or Cycle.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Invalid input or event type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventDto>> Create(EventRequest request)
    {
        var type = EventTypes.Normalise(request.EventType);
        if (type is null) return Fail(400, "EventType must be 'Run', 'Walk' or 'Cycle'.");

        var organiser = await db.Users.FindAsync(CurrentUserId);
        if (organiser is null) return Fail(401, "Authentication required. Please log in.");

        var ev = new Event
        {
            OrganiserId = organiser.UserId,
            Organiser = organiser,
            EventName = request.EventName.Trim(),
            Description = request.Description,
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            EventType = type,
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        return Created($"/api/events/{ev.EventId}", EventDto.From(ev));
    }

    /// <summary>Updates an event. Only the Organiser who created it may do so.</summary>
    /// <response code="200">Updated event.</response>
    /// <response code="400">Invalid input or event type.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Update(int id, EventRequest request)
    {
        var ev = await db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Fail(403, "You can only modify your own events.");

        var type = EventTypes.Normalise(request.EventType);
        if (type is null) return Fail(400, "EventType must be 'Run', 'Walk' or 'Cycle'.");

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate.Date;
        ev.Location = request.Location.Trim();
        ev.EventType = type;
        await db.SaveChangesAsync();

        return Ok(EventDto.From(ev));
    }

    /// <summary>Deletes an event (and its categories). Only the owning Organiser may do so.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">Event has enrolments.</response>
    [HttpDelete("{id:int}")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Fail(403, "You can only delete your own events.");
        if (ev.Categories.Any(c => c.Enrolments.Any()))
            return Fail(409, "This event has enrolments and cannot be deleted.");

        db.Categories.RemoveRange(ev.Categories);
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
