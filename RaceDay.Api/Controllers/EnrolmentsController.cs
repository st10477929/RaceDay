using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class EnrolmentsController(RaceDayDbContext db) : ApiControllerBase
{
    private IQueryable<Enrolment> WithDetails() =>
        db.Enrolments.Include(e => e.Participant).Include(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Enrols the logged-in Participant into a category of an event.</summary>
    /// <remarks>Records the link between the Participant, the event (via the category) and the chosen category.</remarks>
    /// <response code="201">Enrolment created.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Already enrolled in this category, or the category is full.</response>
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [RequireRole(RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enrol(int categoryId)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return Fail(404, "Category not found.");

        var participant = await db.Users.FindAsync(CurrentUserId);
        if (participant is null) return Fail(401, "Authentication required. Please log in.");

        if (await db.Enrolments.AnyAsync(e => e.ParticipantId == participant.UserId && e.CategoryId == categoryId))
            return Fail(409, "You are already enrolled in this category.");

        var enrolled = await db.Enrolments.CountAsync(e => e.CategoryId == categoryId);
        if (enrolled >= category.MaxParticipants) return Fail(409, "This category is full.");

        var enrolment = new Enrolment
        {
            ParticipantId = participant.UserId,
            Participant = participant,
            CategoryId = categoryId,
            Category = category,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Confirmed"
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        return Created("/api/enrolments/me", EnrolmentDto.From(enrolment));
    }

    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/enrolments/me")]
    [RequireRole(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var list = await WithDetails().Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(list.Select(EnrolmentDto.From).ToList());
    }

    /// <summary>Lists all enrolments (across all categories) for an event. Only the owning Organiser may do so.</summary>
    /// <response code="200">Array of enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EnrolmentDto>>> GetForEvent(int eventId)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Fail(403, "You can only view enrolments for your own events.");

        var list = await WithDetails().Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.CategoryId).ThenBy(e => e.EnrolmentDate).ToListAsync();
        return Ok(list.Select(EnrolmentDto.From).ToList());
    }

    /// <summary>Cancels the logged-in Participant's own enrolment.</summary>
    /// <remarks>An enrolment that already has a result captured cannot be cancelled.</remarks>
    /// <response code="204">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("api/enrolments/{id:int}")]
    [RequireRole(RoleNames.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return Fail(404, "Enrolment not found.");
        if (enrolment.ParticipantId != CurrentUserId) return Fail(403, "You can only cancel your own enrolments.");
        if (enrolment.Result is not null) return Fail(409, "A result has already been captured for this enrolment.");

        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
