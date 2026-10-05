using System.Text.Json;
using Fido2NetLib;

namespace Backend.Auth;

// 登録・認証の③で返したオプション一式を、⑩のリクエストが来るまで Session に保持する
public class PendingCeremonyStore(IHttpContextAccessor accessor, Fido2Configuration config)
{
    private record Entry(string OptionsJson, int? UserId, DateTime IssuedAt);

    private ISession Session => accessor.HttpContext!.Session;

    public void Save(string key, string optionsJson, int? userId = null) =>
        Session.SetString(key, JsonSerializer.Serialize(new Entry(optionsJson, userId, DateTime.UtcNow)));

    // 検証の成否にかかわらず使い回させないよう、取り出した時点で削除する
    // 期限切れのものは null を返す（Session 自体の期限はアクセスのたびに延びるので、発行時刻で判定する）
    public (string OptionsJson, int? UserId)? Take(string key)
    {
        var json = Session.GetString(key);
        Session.Remove(key);
        if (json is null) return null;

        var entry = JsonSerializer.Deserialize<Entry>(json)!;
        if (DateTime.UtcNow - entry.IssuedAt > TimeSpan.FromMilliseconds(config.Timeout)) return null;
        return (entry.OptionsJson, entry.UserId);
    }
}
