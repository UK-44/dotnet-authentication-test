using System.Security.Claims;
using Backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Backend.Auth;

// パスワードでもパスキーでも、ログイン後の状態は同じ Cookie 認証にする
public static class SignInExtensions
{
    public static Task SignInUserAsync(this HttpContext context, User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
