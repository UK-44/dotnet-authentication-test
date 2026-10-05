namespace Backend.Models;

// ユーザーに紐づくパスキー（WebAuthn の公開鍵クレデンシャル）
public class PasskeyCredential
{
    public int Id { get; set; }
    public int UserId { get; set; }
    // 認証器が発行するクレデンシャル ID。ログイン時はこれでパスキーを引く
    public required byte[] CredentialId { get; set; }
    // COSE 形式の公開鍵
    public required byte[] PublicKey { get; set; }
    public uint SignCount { get; set; }
    // allowCredentials / excludeCredentials に載せるヒント（"internal,hybrid" のようなカンマ区切り）
    public string? Transports { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
    // 認証器の機種 ID。一覧画面で「iCloud キーチェーン」などを出すのに使える
    public Guid AaGuid { get; set; }
    // ユーザーが一覧で見分けるための名前
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
}
