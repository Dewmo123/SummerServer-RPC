using System.Security.Cryptography;
using System.Text;

namespace SummerProject.Server.Infrastructure.Database;

internal sealed record SqlMigrationProto(
    int Version,
    string Name,
    string Sql,
    string Checksum)
{
    public static SqlMigrationProto Create(int version, string name, string sql)
    {
        // 체크아웃 환경의 줄바꿈 차이가 같은 SQL의 체크섬을 바꾸지 않도록 LF로 정규화한다.
        string normalizedSql = sql.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSql));
        string checksum = Convert.ToHexString(hash).ToLowerInvariant();
        return new SqlMigrationProto(version, name, normalizedSql, checksum);
    }
}