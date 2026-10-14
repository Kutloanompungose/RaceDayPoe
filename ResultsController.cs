using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Tags("Results")]
public class ResultsController(AppDbContext db) : ControllerBase
{
    private ActionResult? Validate(ResultRequest req)
    {
        if (req.Status == "Finished" && req.FinishTime is null)
            return Problem(detail: "A 'Finished' result needs a finishTime.", statusCode: StatusCodes.Status400BadRequest);
        if (req.FinishTime is { } t && t <= TimeSpan.Zero)
            return Problem(detail: "finishTime must be greater than zero.", statusCode: StatusCodes.Status400BadRequest);
        return null;
    }

    /// <summary>Capture a finish time and position for a participant's enrolment.</summary>
    /// <remarks>Expects enrolmentId, status ("Finished", "DNF" or "DNS"), finishTime (e.g. "03:45:02", required when Finished) and position. Organiser only, for events they own.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No enrolment with that id.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("/api/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(CreateResultRequest req)
    {
        if (Validate(req) is { } invalid) return invalid;

        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(e => e.EnrolmentID == req.EnrolmentId);
        if (enrolment is null) return NotFound();

        var organiserId = User.GetUserId();
        if (enrolment.Category.Event.OrganiserID != organiserId) return Forbid();

        if (await db.Results.AnyAsync(r => r.EnrolmentID == req.EnrolmentId))
            return Problem(detail: "A result has already been captured for this enrolment.", statusCode: StatusCodes.Status409Conflict);

        var result = new Result
        {
            EnrolmentID = req.EnrolmentId, FinishTime = req.FinishTime, Position = req.Position,
            Status = req.Status, CapturedByOrganiserID = organiserId
        };
        db.Results.Add(result);
        await db.SaveChangesAsync();

        var dto = await db.Results.Where(r => r.ResultID == result.ResultID).Select(ResultDto.Projection).FirstAsync();
        return Created("/api/results/me", dto);
    }

    /// <summary>Correct a previously captured result.</summary>
    /// <remarks>Expects finishTime, position and status. Organiser only, for events they own.</remarks>
    /// <response code="200">Result updated.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No result with that id.</response>
    [HttpPut("/api/results/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Correct(int id, ResultRequest req)
    {
        if (Validate(req) is { } invalid) return invalid;

        var result = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result is null) return NotFound();
        if (result.Enrolment.Category.Event.OrganiserID != User.GetUserId()) return Forbid();

        result.FinishTime = req.FinishTime;
        result.Position = req.Position;
        result.Status = req.Status;
        await db.SaveChangesAsync();

        return Ok(await db.Results.Where(r => r.ResultID == id).Select(ResultDto.Projection).FirstAsync());
    }

    /// <summary>View your own performance history across all events.</summary>
    /// <response code="200">Array of your results.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("/api/results/me")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        var userId = User.GetUserId();
        var list = await db.Results.Where(r => r.Enrolment.ParticipantID == userId)
            .OrderByDescending(r => r.CapturedAt).Select(ResultDto.Projection).ToListAsync();
        return Ok(list);
    }

    /// <summary>View all captured results for an event.</summary>
    /// <response code="200">Array of results ordered by position.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();
        var list = await db.Results.Where(r => r.Enrolment.Category.EventID == eventId)
            .OrderBy(r => r.Position == null).ThenBy(r => r.Position)
            .Select(ResultDto.Projection).ToListAsync();
        return Ok(list);
    }
}
