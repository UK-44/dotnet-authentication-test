namespace Backend.Models;

public class User
{
    public int Id { get; set; }
    public required string UserName { get; set; }
    public string PasswordHash { get; set; } = "";
    // WebAuthn の user.id。最初のパスキー登録時にランダム値を割り当てる
    public byte[]? UserHandle { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
