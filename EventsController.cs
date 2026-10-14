using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Tags("Events")]
public class EventsController(AppDbContext db) : ControllerBase
{
    /// <summary>List upcoming events so participants can browse them.</summary>
    /// <remarks>Public. Pass includePast=true to also see events that have already taken place.</remarks>
    /// <response code="200">Array of events, soonest first.</response>
    [HttpGet("/api/events")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool includePast = false)
    {
        var query = db.Events.AsQueryable();
        if (!includePast) query = query.Where(e => e.EventDate >= DateTime.UtcNow);
        var events = await query.OrderBy(e => e.EventDate).Select(EventDto.Projection).ToListAsync();
        return Ok(events);
    }

    /// <summary>Get full details of one event, including its categories.</summary>
    /// <response code="200">The event with its categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await db.Events.Where(e => e.EventID == id).Select(e => new EventDetailDto
        {
            EventID = e.EventID, OrganiserID = e.OrganiserID, OrganiserName = e.Organiser.FullName,
            Name = e.Name, Description = e.Description, EventDate = e.EventDate,
            Province = e.Province, Venue = e.Venue, EventType = e.EventType,
            Categories = e.Categories.OrderBy(c => c.DistanceKm).Select(c => new CategoryDto
            {
                CategoryID = c.CategoryID, EventID = c.EventID, Name = c.Name,
                DistanceKm = c.DistanceKm, MaxParticipants = c.MaxParticipants, EntryFee = c.EntryFee
            }).ToList()
        }).FirstOrDefaultAsync();

        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>Create a new event owned by the logged-in Organiser.</summary>
    /// <remarks>Expects name, eventDate, province, venue, eventType ("Run", "Walk" or "Cycle") and an optional description. Organiser only.</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost("/api/events")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(EventRequest req)
    {
        var ev = new Event
        {
            OrganiserID = User.GetUserId(),
            Name = req.Name.Trim(), Description = req.Description,
            EventDate = req.EventDate!.Value, Province = req.Province.Trim(),
            Venue = req.Venue.Trim(), EventType = req.EventType
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var dto = await db.Events.Where(e => e.EventID == ev.EventID).Select(EventDto.Projection).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventID }, dto);
    }

    /// <summary>Update an event you own.</summary>
    /// <remarks>Expects the same body as event creation. Organiser only, and only for their own events.</remarks>
    /// <response code="200">Event updated.</response>
    /// <response code="403">You are not the organiser of this event (or you are a Participant).</response>
    /// <response code="404">No event with that id.</response>
    [HttpPut("/api/events/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, EventRequest req)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        ev.Name = req.Name.Trim();
        ev.Description = req.Description;
        ev.EventDate = req.EventDate!.Value;
        ev.Province = req.Province.Trim();
        ev.Venue = req.Venue.Trim();
        ev.EventType = req.EventType;
        await db.SaveChangesAsync();

        return Ok(await db.Events.Where(e => e.EventID == id).Select(EventDto.Projection).FirstAsync());
    }

    /// <summary>Delete an event you own, together with its categories, enrolments and results.</summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="403">You are not the organiser of this event (or you are a Participant).</response>
    /// <response code="404">No event with that id.</response>
    [HttpDelete("/api/events/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        // Enrolments reference categories without cascade, so remove them first (results cascade from enrolments).
        var enrolments = await db.Enrolments.Where(e => e.Category.EventID == id).ToListAsync();
        db.Enrolments.RemoveRange(enrolments);
        await db.SaveChangesAsync();

        db.Events.Remove(ev);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>List every enrolment across all categories of an event you own.</summary>
    /// <remarks>Organiser only, and only for their own events. Used to manage race day.</remarks>
    /// <response code="200">Array of enrolments with participant and category names.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{id:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnrolments(int id)
    {
        var ev = await db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var list = await db.Enrolments.Where(e => e.Category.EventID == id)
            .OrderBy(e => e.CategoryID).ThenBy(e => e.EnrolmentID)
            .Select(EnrolmentDto.Projection).ToListAsync();
        return Ok(list);
    }
}
