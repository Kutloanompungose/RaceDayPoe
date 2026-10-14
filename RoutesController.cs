using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Tags("Route Info")]
public class RoutesController(AppDbContext db) : ControllerBase
{
    /// <summary>Get the route details for a category so participants can prepare.</summary>
    /// <response code="200">The route.</response>
    /// <response code="404">The category has no route (or does not exist).</response>
    [HttpGet("/api/categories/{categoryId:int}/route")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int categoryId)
    {
        var route = await db.RouteInfos.FirstOrDefaultAsync(r => r.CategoryID == categoryId);
        return route is null ? NotFound() : Ok(RouteDto.From(route));
    }

    /// <summary>Attach route information to a category of an event you own.</summary>
    /// <remarks>Expects startPoint, endPoint, and optionally routeMapUrl, elevationGainM and description. One route per category.</remarks>
    /// <response code="201">Route created.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No category with that id.</response>
    /// <response code="409">This category already has a route.</response>
    [HttpPost("/api/categories/{categoryId:int}/route")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int categoryId, RouteRequest req)
    {
        var cat = await db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == categoryId);
        if (cat is null) return NotFound();
        if (cat.Event.OrganiserID != User.GetUserId()) return Forbid();
        if (await db.RouteInfos.AnyAsync(r => r.CategoryID == categoryId))
            return Problem(detail: "This category already has a route. Use PUT /api/routes/{id} to change it.", statusCode: StatusCodes.Status409Conflict);

        var route = new RouteInfo
        {
            CategoryID = categoryId, RouteMapURL = req.RouteMapUrl, StartPoint = req.StartPoint.Trim(),
            EndPoint = req.EndPoint.Trim(), ElevationGainM = req.ElevationGainM ?? 0, Description = req.Description
        };
        db.RouteInfos.Add(route);
        await db.SaveChangesAsync();
        return Created($"/api/categories/{categoryId}/route", RouteDto.From(route));
    }

    /// <summary>Update route information of a category you own.</summary>
    /// <response code="200">Route updated.</response>
    /// <response code="403">Not the organiser of this event (or a Participant).</response>
    /// <response code="404">No route with that id.</response>
    [HttpPut("/api/routes/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(RouteDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, RouteRequest req)
    {
        var route = await db.RouteInfos.Include(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.RouteID == id);
        if (route is null) return NotFound();
        if (route.Category.Event.OrganiserID != User.GetUserId()) return Forbid();

        route.RouteMapURL = req.RouteMapUrl;
        route.StartPoint = req.StartPoint.Trim();
        route.EndPoint = req.EndPoint.Trim();
        route.ElevationGainM = req.ElevationGainM ?? 0;
        route.Description = req.Description;
        await db.SaveChangesAsync();
        return Ok(RouteDto.From(route));
    }
}
