using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
[Tags("Users")]
public class UsersController(AppDbContext db) : ControllerBase
{
    /// <summary>View your own profile (Organiser or Participant).</summary>
    /// <response code="200">Returns your profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("/api/users/me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var user = await db.Users.FindAsync(User.GetUserId());
        return user is null ? NotFound() : Ok(UserDto.From(user));
    }

    /// <summary>Update your own profile details (Organiser or Participant).</summary>
    /// <remarks>Expects fullName (required) and phone (optional). Email and role cannot be changed here.</remarks>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("/api/users/me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest req)
    {
        var user = await db.Users.FindAsync(User.GetUserId());
        if (user is null) return NotFound();

        user.FullName = req.FullName.Trim();
        user.Phone = req.Phone?.Trim();
        await db.SaveChangesAsync();
        return Ok(UserDto.From(user));
    }
}
