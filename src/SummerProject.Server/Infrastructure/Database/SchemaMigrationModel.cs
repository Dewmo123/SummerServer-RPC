namespace SummerProject.Server.Infrastructure.Database;

// 적용된 SQL 파일의 이름과 체크섬을 SQLite 행 그대로 표현한다.
internal sealed record SchemaMigrationModel(
    long Version,
    string Name,
    string Checksum,
    long AppliedAtUtcMs);