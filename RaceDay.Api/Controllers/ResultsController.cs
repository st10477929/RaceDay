using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class ResultsController(RaceDayDbContext db) : ApiControllerBase
{
    private IQueryable<Result> WithDetails() =>
        db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event);

    /// <summary>Captures the finish time and finishing position for a Participant's enrolment.</summary>
    /// <remarks>Only the Organiser who owns the event may capture results. One result per enrolment.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(int enrolmentId, ResultRequest request)
    {
        var enrolment = await db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return Fail(404, "Enrolment not found.");
        if (enrolment.Category.Event.OrganiserId != CurrentUserId)
            return Fail(403, "You can only capture results for your own events.");
        if (enrolment.Result is not null) return Fail(409, "A result already exists for this enrolment.");

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            Enrolment = enrolment,
            FinishTime = request.FinishTime,
            Position = request.Position,
            CapturedByOrganiserId = CurrentUserId,
            CapturedAt = DateTime.UtcNow
        };
        db.Results.Add(result);
        await db.SaveChangesAsync();

        return Created($"/api/events/{enrolment.Category.EventId}/results", ResultDto.From(result));
    }

    /// <summary>Updates a captured result. Only the Organiser who owns the event may do so.</summary>
    /// <response code="200">Updated result.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("api/results/{id:int}")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, ResultRequest request)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return Fail(404, "Result not found.");
        if (result.Enrolment.Category.Event.OrganiserId != CurrentUserId)
            return Fail(403, "You can only update results for your own events.");

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        await db.SaveChangesAsync();

        return Ok(ResultDto.From(result));
    }

    /// <summary>Lists the logged-in Participant's own results.</summary>
    /// <response code="200">Array of results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("api/results/me")]
    [RequireRole(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetMine()
    {
        var userId = CurrentUserId;
        var list = await WithDetails().Where(r => r.Enrolment.ParticipantId == userId)
            .OrderByDescending(r => r.CapturedAt).ToListAsync();
        return Ok(list.Select(ResultDto.From).ToList());
    }

    /// <summary>Returns the results (leaderboard) for an event, ordered by category then position. Public.</summary>
    /// <response code="200">Array of results.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ResultDto>>> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return Fail(404, "Event not found.");

        var list = await WithDetails().Where(r => r.Enrolment.Category.EventId == eventId)
            .OrderBy(r => r.Enrolment.CategoryId).ThenBy(r => r.Position).ToListAsync();
        return Ok(list.Select(ResultDto.From).ToList());
    }
}
