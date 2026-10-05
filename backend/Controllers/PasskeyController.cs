using System.Buffers.Text;
using System.Security.Claims;
using System.Security.Cryptography;
using Backend.Auth;
using Backend.Data;
using Backend.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/passkeys")]
public class PasskeyController(
    AppDbContext db,
    IFido2 fido2,
    Fido2Configuration fido2Config,
    PendingCeremonyStore pending,
    IPasswordHasher<User> hasher) : ControllerBase
{
    private const string RegistrationKey = "passkey.registration";
    private const string LoginKey = "passkey.login";

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize]
    [HttpGet]
    public async Task<PasskeyListResponse> List()
    {
        var userHandle = await db.Users
            .Where(u => u.Id == CurrentUserId)
            .Select(u => u.UserHandle)
            .SingleAsync();
        var passkeys = await db.PasskeyCredentials
            .Where(c => c.UserId == CurrentUserId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        return new PasskeyListResponse(
            fido2Config.RPID,
            userHandle is null ? null : Base64Url.EncodeToString(userHandle),
            passkeys.Select(ToResponse).ToList());
    }

    // 登録の②: パスワードを確認してから、パスキーの作成に必要な情報を返す
    [Authorize]
    [HttpPost("register/options")]
    public async Task<IActionResult> RegisterOptions(PasskeyRegisterOptionsRequest req)
    {
        var user = await db.Users.SingleAsync(u => u.Id == CurrentUserId);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "パスワードが正しくありません" });

        // user handle は最初のパスキー登録時に割り当てる（仕様の推奨どおり 64 バイトのランダム値）
        if (user.UserHandle is null)
        {
            user.UserHandle = RandomNumberGenerator.GetBytes(64);
            await db.SaveChangesAsync();
        }

        // 登録済みのパスキーを渡し、同じ認証器への二重登録を防ぐ
        var existing = await db.PasskeyCredentials
            .Where(c => c.UserId == user.Id)
            .Select(c => new { c.CredentialId, c.Transports })
            .ToListAsync();

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User { Id = user.UserHandle, Name = user.UserName, DisplayName = user.UserName },
            ExcludeCredentials = existing.Select(c => ToDescriptor(c.CredentialId, c.Transports)).ToList(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                // ユーザー名を入力せずにログインできるよう、discoverable credential を作らせる
                ResidentKey = ResidentKeyRequirement.Required,
                UserVerification = UserVerificationRequirement.Required,
            },
            AttestationPreference = AttestationConveyancePreference.None,
        });

        var json = options.ToJson();
        pending.Save(RegistrationKey, json, user.Id);
        return Content(json, "application/json");
    }

    // 登録の⑪: 検証して公開鍵を保存する
    [Authorize]
    [HttpPost("register")]
    public async Task<ActionResult<PasskeyResponse>> Register(
        AuthenticatorAttestationRawResponse credential, CancellationToken ct)
    {
        // オプションを発行したときのユーザーと、今ログインしているユーザーが同じか確認する
        if (pending.Take(RegistrationKey) is not { } entry || entry.UserId != CurrentUserId)
            return BadRequest(new { message = "登録の有効期限が切れました。もう一度お試しください" });

        RegisteredPublicKeyCredential result;
        try
        {
            result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = credential,
                OriginalOptions = CredentialCreateOptions.FromJson(entry.OptionsJson),
                IsCredentialIdUniqueToUserCallback = async (args, token) =>
                    !await db.PasskeyCredentials.AnyAsync(c => c.CredentialId == args.CredentialId, token),
            }, ct);
        }
        catch (Fido2VerificationException)
        {
            return BadRequest(new { message = "パスキーを登録できませんでした" });
        }

        var count = await db.PasskeyCredentials.CountAsync(c => c.UserId == CurrentUserId, ct);
        var passkey = new PasskeyCredential
        {
            UserId = CurrentUserId,
            CredentialId = result.Id,
            PublicKey = result.PublicKey,
            SignCount = result.SignCount,
            Transports = result.Transports is { Length: > 0 }
                ? string.Join(',', result.Transports.Select(t => t.ToEnumMemberValue()))
                : null,
            IsBackupEligible = result.IsBackupEligible,
            IsBackedUp = result.IsBackedUp,
            AaGuid = result.AaGuid,
            Name = $"パスキー {count + 1}",
        };
        db.PasskeyCredentials.Add(passkey);
        await db.SaveChangesAsync(ct);

        return ToResponse(passkey);
    }

    // 認証の②: ユーザー名を入力しないログインなので、allowCredentials は指定しない
    [EnableRateLimiting(RateLimitPolicies.PasskeyLogin)]
    [HttpPost("login/options")]
    public IActionResult LoginOptions()
    {
        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            UserVerification = UserVerificationRequirement.Required,
        });

        var json = options.ToJson();
        pending.Save(LoginKey, json);
        return Content(json, "application/json");
    }

    // 認証の⑪: 署名を検証してログインさせる
    [HttpPost("login")]
    public async Task<ActionResult<UserResponse>> Login(
        AuthenticatorAssertionRawResponse credential, CancellationToken ct)
    {
        if (pending.Take(LoginKey) is not { } entry)
            return BadRequest(new { message = "ログインの有効期限が切れました。もう一度お試しください" });

        var failed = Unauthorized(new { message = "パスキーでログインできませんでした" });

        // user handle が返ってこないと、fido2-net-lib は持ち主の確認（コールバック）自体を呼ばない
        if (credential.Response?.UserHandle is not { Length: > 0 })
            return failed;

        var passkey = await db.PasskeyCredentials.SingleOrDefaultAsync(c => c.CredentialId == credential.RawId, ct);
        if (passkey is null) return failed;
        var owner = await db.Users.SingleAsync(u => u.Id == passkey.UserId, ct);

        VerifyAssertionResult result;
        try
        {
            result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = credential,
                OriginalOptions = AssertionOptions.FromJson(entry.OptionsJson),
                StoredPublicKey = passkey.PublicKey,
                StoredSignatureCounter = passkey.SignCount,
                // 返ってきた user handle が、このパスキーの持ち主のものか
                IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(
                    owner.UserHandle is not null && args.UserHandle.AsSpan().SequenceEqual(owner.UserHandle)),
            }, ct);
        }
        catch (Fido2VerificationException)
        {
            return failed;
        }

        // BE（同期パスキーになりうるか）は登録時から変わらないはずなので、変わっていたら受け付けない
        // fido2-net-lib はこの比較をしないので、署名の検証が済んだ認証データから取り出して比べる
        var authData = AuthenticatorData.Parse(credential.Response.AuthenticatorData);
        if (authData.IsBackupEligible != passkey.IsBackupEligible) return failed;

        passkey.SignCount = result.SignCount;
        passkey.IsBackedUp = result.IsBackedUp;
        passkey.LastUsedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await HttpContext.SignInUserAsync(owner);
        return new UserResponse(owner.Id, owner.UserName);
    }

    [Authorize]
    [HttpPatch("{id:int}")]
    public async Task<ActionResult<PasskeyResponse>> Rename(int id, PasskeyRenameRequest req)
    {
        var passkey = await db.PasskeyCredentials.SingleOrDefaultAsync(c => c.Id == id && c.UserId == CurrentUserId);
        if (passkey is null) return NotFound();

        passkey.Name = req.Name.Trim();
        await db.SaveChangesAsync();
        return ToResponse(passkey);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await db.PasskeyCredentials
            .Where(c => c.Id == id && c.UserId == CurrentUserId)
            .ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }

    private static PublicKeyCredentialDescriptor ToDescriptor(byte[] credentialId, string? transports) =>
        new(PublicKeyCredentialType.PublicKey, credentialId,
            transports?.Split(',').Select(t => t.ToEnum<AuthenticatorTransport>()).ToArray());

    private static PasskeyResponse ToResponse(PasskeyCredential c) =>
        new(c.Id, Base64Url.EncodeToString(c.CredentialId), c.Name, c.IsBackedUp, AsUtc(c.CreatedAt), c.LastUsedAt is { } t ? AsUtc(t) : null);

    // MySQL の datetime はタイムゾーンを持たず、読み出した値は Kind が Unspecified になるので、UTC として JSON に出す
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
