using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Tags("Categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    /// <summary>List the categories (e.g. 10km, 21km) offered under an event.</summary>
    /// <response code="200">Array of categories.</response>
    /// <response code="404">No event with that id.</response>
    [HttpGet("/api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventID == eventId)) return NotFound();
        var list = await db.Categories.Where(c => c.EventID == eventId)
            .OrderBy(c => c.DistanceKm).Select(CategoryDto.Projection).ToListAsync();
        return Ok(list);
    }

    /// <summary>Add a category (distance) to an event you own.</summary>
    /// <remarks>Expects name, distanceKm, maxParticipants and entryFee. Organiser only.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No event with that id.</response>
    [HttpPost("/api/events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CategoryRequest req)
    {
        var ev = await db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserID != User.GetUserId()) return Forbid();

        var cat = new Category
        {
            EventID = eventId, Name = req.Name.Trim(), DistanceKm = req.DistanceKm,
            MaxParticipants = req.MaxParticipants, EntryFee = req.EntryFee
        };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        var dto = await db.Categories.Where(c => c.CategoryID == cat.CategoryID).Select(CategoryDto.Projection).FirstAsync();
        return Created($"/api/events/{eventId}/categories", dto);
    }

    /// <summary>Update a category of an event you own.</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No category with that id.</response>
    [HttpPut("/api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CategoryRequest req)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserID != User.GetUserId()) return Forbid();

        cat.Name = req.Name.Trim();
        cat.DistanceKm = req.DistanceKm;
        cat.MaxParticipants = req.MaxParticipants;
        cat.EntryFee = req.EntryFee;
        await db.SaveChangesAsync();

        return Ok(await db.Categories.Where(c => c.CategoryID == id).Select(CategoryDto.Projection).FirstAsync());
    }

    /// <summary>Remove a category (and its enrolments, results and route) from an event you own.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No category with that id.</response>
    [HttpDelete("/api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserID != User.GetUserId()) return Forbid();

        var enrolments = await db.Enrolments.Where(e => e.CategoryID == id).ToListAsync();
        db.Enrolments.RemoveRange(enrolments);
        await db.SaveChangesAsync();

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
