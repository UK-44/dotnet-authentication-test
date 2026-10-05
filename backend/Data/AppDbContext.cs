using Backend.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();

    // ログインセッション（Cookie にはこのテーブルのキーだけを載せる）
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<PasskeyCredential> PasskeyCredentials => Set<PasskeyCredential>();

    // Cookie 暗号化キーの保存先（コンテナ再起動でログインが切れないように）
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.UserName).HasMaxLength(32);
            e.HasIndex(u => u.UserName).IsUnique();
            // パスキーでのログイン時に userHandle からユーザーを引く
            e.Property(u => u.UserHandle).HasMaxLength(64);
            e.HasIndex(u => u.UserHandle).IsUnique();
        });

        modelBuilder.Entity<UserSession>(e =>
        {
            e.Property(s => s.Id).HasMaxLength(64);
            // ユーザー削除時にセッションも消え、即座にログアウトされる
            e.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasskeyCredential>(e =>
        {
            // WebAuthn の仕様上、クレデンシャル ID は最大 1023 バイト
            e.Property(c => c.CredentialId).HasMaxLength(1023);
            e.HasIndex(c => c.CredentialId).IsUnique();
            e.Property(c => c.Transports).HasMaxLength(128);
            e.Property(c => c.Name).HasMaxLength(64);
            e.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
