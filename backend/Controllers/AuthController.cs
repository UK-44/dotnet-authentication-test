using System.Security.Claims;
using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IPasswordHasher<User> hasher) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.UserName == req.UserName))
            return Conflict(new { message = "このユーザー名は既に使われています" });

        var user = new User { UserName = req.UserName };
        user.PasswordHash = hasher.HashPassword(user, req.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await HttpContext.SignInUserAsync(user);
        return new UserResponse(user.Id, user.UserName);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest req)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == req.UserName);
        if (user is null ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "ユーザー名またはパスワードが正しくありません" });

        await HttpContext.SignInUserAsync(user);
        return new UserResponse(user.Id, user.UserName);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return new UserResponse(id, User.Identity!.Name!);
    }
}
