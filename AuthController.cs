using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Tags("Authentication")]
public class AuthController(AppDbContext db, TokenService tokens) : ControllerBase
{
    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>Expects fullName, email, password (min 8 characters) and role ("Organiser" or "Participant"). The password is stored as a BCrypt hash, never in plain text.</remarks>
    /// <response code="201">Account created. Returns the user profile.</response>
    /// <response code="400">Validation failed (e.g. invalid email, short password, unknown role).</response>
    /// <response code="409">That email address is already registered.</response>
    [HttpPost("/api/auth/register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Problem(detail: "That email address is already registered.", statusCode: StatusCodes.Status409Conflict);

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = req.Role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("/api/users/me", UserDto.From(user));
    }

    /// <summary>Log in and receive a JWT access token.</summary>
    /// <remarks>Expects email and password. Send the returned token as "Authorization: Bearer {token}" on later requests; it carries your user id and role.</remarks>
    /// <response code="200">Login successful. Returns the token, its expiry time and the user profile.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("/api/auth/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Problem(detail: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        var (token, expires) = tokens.Create(user);
        return Ok(new LoginResponse { Token = token, ExpiresAt = expires, User = UserDto.From(user) });
    }
}
