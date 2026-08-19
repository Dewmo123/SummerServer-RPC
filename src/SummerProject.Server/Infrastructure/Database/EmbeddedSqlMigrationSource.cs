using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace SummerProject.Server.Infrastructure.Database;

internal sealed partial class EmbeddedSqlMigrationSource
{
    private readonly Assembly _assembly = typeof(EmbeddedSqlMigrationSource).Assembly;

    public async ValueTask<IReadOnlyList<SqlMigrationProto>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        string resourcePrefix = $"{typeof(EmbeddedSqlMigrationSource).Namespace}.Migrations.";
        string[] resourceNames = _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new SqliteMigrationException("내장된 SQL 마이그레이션을 찾을 수 없습니다.");
        }

        List<SqlMigrationProto> migrations = new(resourceNames.Length);
        foreach (string resourceName in resourceNames)
        {
            string fileName = resourceName[resourcePrefix.Length..];
            Match match = MigrationFileName().Match(fileName);
            if (!match.Success
                || !int.TryParse(
                    match.Groups["version"].Value,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int version))
            {
                throw new SqliteMigrationException($"SQL 마이그레이션 파일명이 유효하지 않습니다: {fileName}");
            }

            await using Stream stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new SqliteMigrationException($"SQL 마이그레이션을 읽을 수 없습니다: {fileName}");
            using StreamReader reader = new(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true),
                detectEncodingFromByteOrderMarks: true);
            string sql = await reader.ReadToEndAsync(cancellationToken);
            migrations.Add(SqlMigrationProto.Create(version, fileName, sql));
        }

        int duplicatedVersion = migrations
            .GroupBy(migration => migration.Version)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .FirstOrDefault();
        if (duplicatedVersion != 0)
        {
            throw new SqliteMigrationException($"SQL 마이그레이션 버전이 중복되었습니다: {duplicatedVersion:D4}");
        }

        return migrations;
    }

    [GeneratedRegex("^(?<version>[0-9]{4})_[a-z0-9_]+[.]sql$", RegexOptions.CultureInvariant)]
    private static partial Regex MigrationFileName();
}