using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Participant)]
[Tags("Event Enrolments")]
public class EnrolmentsController(AppDbContext db) : ControllerBase
{
    /// <summary>Enter an event by choosing one of its categories.</summary>
    /// <remarks>Expects categoryId. The link between you, the event and the category is stored and a race number is assigned. Participant only.</remarks>
    /// <response code="201">Enrolled. Returns the enrolment including your race number.</response>
    /// <response code="400">The event has already taken place, or the body is invalid.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">You are already enrolled in this category, or the category is full.</response>
    [HttpPost("/api/enrolments")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(EnrolRequest req)
    {
        var userId = User.GetUserId();
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == req.CategoryId);
        if (cat is null) return NotFound();

        if (cat.Event.EventDate < DateTime.UtcNow)
            return Problem(detail: "This event has already taken place.", statusCode: StatusCodes.Status400BadRequest);

        if (await db.Enrolments.AnyAsync(e => e.ParticipantID == userId && e.CategoryID == req.CategoryId))
            return Problem(detail: "You are already enrolled in this category.", statusCode: StatusCodes.Status409Conflict);

        var taken = await db.Enrolments.CountAsync(e => e.CategoryID == req.CategoryId && e.Status != "Cancelled");
        if (taken >= cat.MaxParticipants)
            return Problem(detail: "This category is full.", statusCode: StatusCodes.Status409Conflict);

        var enrolment = new Enrolment { ParticipantID = userId, CategoryID = req.CategoryId, Status = "Confirmed" };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();

        enrolment.RaceNumber = $"R{enrolment.EnrolmentID:D5}"; // unique, max 10 chars
        await db.SaveChangesAsync();

        var dto = await db.Enrolments.Where(e => e.EnrolmentID == enrolment.EnrolmentID).Select(EnrolmentDto.Projection).FirstAsync();
        return Created("/api/enrolments/me", dto);
    }

    /// <summary>View your own enrolment history.</summary>
    /// <response code="200">Array of your enrolments, newest first.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("/api/enrolments/me")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var userId = User.GetUserId();
        var list = await db.Enrolments.Where(e => e.ParticipantID == userId)
            .OrderByDescending(e => e.EnrolmentDate).Select(EnrolmentDto.Projection).ToListAsync();
        return Ok(list);
    }

    /// <summary>Cancel one of your own enrolments.</summary>
    /// <response code="204">Enrolment cancelled (removed).</response>
    /// <response code="403">That enrolment belongs to someone else, or you are an Organiser.</response>
    /// <response code="404">No enrolment with that id.</response>
    [HttpDelete("/api/enrolments/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await db.Enrolments.FindAsync(id);
        if (enrolment is null) return NotFound();
        if (enrolment.ParticipantID != User.GetUserId()) return Forbid();

        db.Enrolments.Remove(enrolment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
