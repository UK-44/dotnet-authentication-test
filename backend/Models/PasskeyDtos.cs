using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

// パスキー追加前の再認証のため、パスワードを再入力させる
public record PasskeyRegisterOptionsRequest([Required] string Password);

public record PasskeyRenameRequest([Required, StringLength(64, MinimumLength = 1)] string Name);

public record PasskeyResponse(
    int Id,
    string CredentialId,
    string? Name,
    bool IsBackedUp,
    DateTime CreatedAt,
    DateTime? LastUsedAt);

// RpId・UserHandle は、ブラウザに有効なパスキーの一覧を伝える（signalAllAcceptedCredentials）ために返す
public record PasskeyListResponse(string RpId, string? UserHandle, List<PasskeyResponse> Passkeys);
