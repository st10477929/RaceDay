using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

public class CategoriesController(RaceDayDbContext db) : ApiControllerBase
{
    /// <summary>Lists the categories of an event. Public.</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return Fail(404, "Event not found.");

        var categories = await db.Categories.Where(c => c.EventId == eventId)
            .OrderBy(c => c.Distance).ToListAsync();
        return Ok(categories.Select(CategoryDto.From).ToList());
    }

    /// <summary>Adds an age or distance category to an event. Only the owning Organiser may do so.</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">A category with this name already exists for the event.</response>
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequest request)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return Fail(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return Fail(403, "You can only add categories to your own events.");

        var name = request.CategoryName.Trim();
        if (await db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
            return Fail(409, "A category with this name already exists for this event.");

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            Distance = request.Distance,
            MaxParticipants = request.MaxParticipants,
            EntryFee = request.EntryFee
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", CategoryDto.From(category));
    }

    /// <summary>Updates a category. Only the Organiser who owns the event may do so.</summary>
    /// <response code="200">Updated category.</response>
    /// <response code="400">Invalid input.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("api/categories/{id:int}")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
    {
        var category = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return Fail(404, "Category not found.");
        if (category.Event.OrganiserId != CurrentUserId) return Fail(403, "You can only modify categories of your own events.");

        category.CategoryName = request.CategoryName.Trim();
        category.Distance = request.Distance;
        category.MaxParticipants = request.MaxParticipants;
        category.EntryFee = request.EntryFee;
        await db.SaveChangesAsync();

        return Ok(CategoryDto.From(category));
    }

    /// <summary>Deletes a category. Only the Organiser who owns the event may do so.</summary>
    /// <remarks>A category that already has enrolments cannot be deleted.</remarks>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category has enrolments.</response>
    [HttpDelete("api/categories/{id:int}")]
    [RequireRole(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await db.Categories.Include(c => c.Event).Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return Fail(404, "Category not found.");
        if (category.Event.OrganiserId != CurrentUserId) return Fail(403, "You can only delete categories of your own events.");
        if (category.Enrolments.Any()) return Fail(409, "This category has enrolments and cannot be deleted.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
