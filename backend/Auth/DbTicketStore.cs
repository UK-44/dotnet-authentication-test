using System.Security.Claims;
using System.Security.Cryptography;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace Backend.Auth;

// 認証チケットを DB に保存し、Cookie にはセッションキーだけを載せる
// ITicketStore は Singleton なので DbContext はスコープを作って都度取得する
public class DbTicketStore(IServiceScopeFactory scopeFactory) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var session = new UserSession
        {
            Id = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            UserId = int.Parse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Ticket = TicketSerializer.Default.Serialize(ticket),
            ExpiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime,
        };

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    // スライディング期限の延長時に呼ばれる
    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        var bytes = TicketSerializer.Default.Serialize(ticket);
        var expiresAt = ticket.Properties.ExpiresUtc?.UtcDateTime;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.UserSessions
            .Where(s => s.Id == key)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Ticket, bytes)
                .SetProperty(x => x.ExpiresAt, expiresAt));
    }

    // null を返すと未認証扱いになる（期限切れの判定はハンドラー側で行われる）
    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var session = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(s => s.Id == key);
        return session is null ? null : TicketSerializer.Default.Deserialize(session.Ticket);
    }

    // SignOutAsync や期限切れ検出時に呼ばれる
    public async Task RemoveAsync(string key)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.UserSessions.Where(s => s.Id == key).ExecuteDeleteAsync();
    }
}
