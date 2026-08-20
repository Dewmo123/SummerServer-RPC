using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SummerProject.Server.Helpers.Auth;

internal sealed class GoogleUsernameFactory
{
    private const int MaximumAttempts = 8;

    public IEnumerable<string> CreateCandidates(string subject)
    {
        for (int attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            // 외부 식별자를 노출하지 않으면서 충돌 시에도 같은 입력에서 같은 후보 순서를 만든다.
            string source = string.Create(
                CultureInfo.InvariantCulture,
                $"google:{attempt}:{subject}");
            string hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            yield return $"g_{hash[..48]}";
        }
    }
}