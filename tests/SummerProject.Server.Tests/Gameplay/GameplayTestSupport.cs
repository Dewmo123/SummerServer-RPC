using System.Text.Json;

using Microsoft.Data.Sqlite;

using SummerProject.Server.Tests.Auth;

namespace SummerProject.Server.Tests.Gameplay;

internal sealed record GameplayTestSession(
    long UserId,
    string AccessToken);

internal static class GameplayTestSupport
{
    public static async Task<GameplayTestSession> LoginAsync(
        HttpClient client,
        string subject)
    {
        using JsonDocument document = await AuthRpcClient.PostAsync(
            client,
            "auth.login.google",
            new { idToken = $"valid:{subject}" });
        JsonElement result = document.RootElement.GetProperty("result");
        return new GameplayTestSession(
            result.GetProperty("userId").GetInt64(),
            AuthRpcClient.ReadAccessToken(result));
    }

    public static SqliteConnection Open(string databasePath)
    {
        SqliteConnection connection = new(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        connection.Open();
        return connection;
    }
}