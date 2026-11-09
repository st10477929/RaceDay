using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;
using AppUser = RaceDay.Api.Models.User;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController(RaceDayDbContext db, IPasswordService passwords) : ApiControllerBase
{
    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Only the two valid roles are accepted; any other role value is rejected.
    /// The password is hashed (BCrypt) before it is stored and is never returned.
    /// </remarks>
    /// <response code="201">Account created. Returns the user (no password).</response>
    /// <response code="400">Invalid input or invalid role.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        var roleName = RoleNames.Normalise(request.Role);
        if (roleName is null) return Fail(400, "Role must be either 'Organiser' or 'Participant'.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
            return Fail(409, "An account with this email already exists.");

        var role = await db.Roles.SingleAsync(r => r.RoleName == roleName);
        var user = new AppUser
        {
            RoleId = role.RoleId,
            Role = role,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwords.Hash(request.Password),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Created("/api/users/me", UserDto.From(user));
    }

    /// <summary>Logs in with email and password and starts a server-side session.</summary>
    /// <remarks>On success the user ID and role are stored in the session; the browser receives a session cookie.</remarks>
    /// <response code="200">Login successful. Returns the user.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            return Fail(401, "Invalid email or password.");

        HttpContext.Session.SignIn(user.UserId, user.Role.RoleName);
        return Ok(UserDto.From(user));
    }

    /// <summary>Logs out and clears the server-side session.</summary>
    /// <response code="204">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
