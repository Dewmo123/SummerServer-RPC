using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Models.Characters;
using SummerProject.Server.Services.Characters;
using SummerProject.Server.Tests.Auth;
using SummerProject.Server.Tests.Gameplay;

namespace SummerProject.Server.Tests.Characters;

public sealed class CharacterProgressionServiceTests
{
    [Fact]
    public async Task ExperienceCarriesAcrossExactAndMultipleLevelBoundaries()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-progress-user");
        using IServiceScope scope = application.Services.CreateScope();
        CharacterProgressionService service =
            scope.ServiceProvider.GetRequiredService<CharacterProgressionService>();

        CharacterProto beforeBoundary = await service.AddExperienceAsync(
            session.UserId,
            99,
            CancellationToken.None);
        CharacterProto exactBoundary = await service.AddExperienceAsync(
            session.UserId,
            1,
            CancellationToken.None);
        CharacterProto multipleLevels = await service.AddExperienceAsync(
            session.UserId,
            550,
            CancellationToken.None);

        Assert.Equal(new CharacterProto(1, 99, 100), beforeBoundary);
        Assert.Equal(new CharacterProto(2, 0, 200), exactBoundary);
        Assert.Equal(new CharacterProto(4, 50, 400), multipleLevels);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveExperienceIsRejected(long amount)
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            $"character-invalid-exp-{amount}");
        using IServiceScope scope = application.Services.CreateScope();
        CharacterProgressionService service =
            scope.ServiceProvider.GetRequiredService<CharacterProgressionService>();

        await Assert.ThrowsAsync<CharacterInvalidExperienceException>(() =>
            service.AddExperienceAsync(session.UserId, amount, CancellationToken.None).AsTask());

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        Assert.Equal(0L, await connection.QuerySingleAsync<long>(
            "SELECT COUNT(*) FROM characters WHERE user_id = @UserId;",
            new { session.UserId }));
    }

    [Fact]
    public async Task OverflowRollsBackEntireCharacterChange()
    {
        const long level = long.MaxValue / 100;
        const long exp = (level * 100) - 1;
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-overflow-user");
        using IServiceScope scope = application.Services.CreateScope();
        CharacterQueryService queryService =
            scope.ServiceProvider.GetRequiredService<CharacterQueryService>();
        CharacterProgressionService progressionService =
            scope.ServiceProvider.GetRequiredService<CharacterProgressionService>();
        _ = await queryService.GetMineAsync(session.UserId, CancellationToken.None);

        await using (SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath))
        {
            await connection.ExecuteAsync(
                "UPDATE characters SET level = @Level, exp = @Exp WHERE user_id = @UserId;",
                new { Level = level, Exp = exp, session.UserId });
        }

        await Assert.ThrowsAsync<CharacterInvalidExperienceException>(() =>
            progressionService.AddExperienceAsync(session.UserId, 1, CancellationToken.None).AsTask());

        await using SqliteConnection verification = GameplayTestSupport.Open(application.DatabasePath);
        dynamic character = await verification.QuerySingleAsync(
            "SELECT level, exp FROM characters WHERE user_id = @UserId;",
            new { session.UserId });
        Assert.Equal(level, (long)character.level);
        Assert.Equal(exp, (long)character.exp);
    }

    [Fact]
    public async Task ConcurrentExperienceChangesAreSerializedWithoutLoss()
    {
        await using AuthenticationTestApplicationFactory application = new();
        using HttpClient client = application.CreateClient();
        GameplayTestSession session = await GameplayTestSupport.LoginAsync(
            client,
            "character-exp-race-user");

        Task<CharacterProto>[] changes = Enumerable.Range(0, 2)
            .Select(async _ =>
            {
                using IServiceScope scope = application.Services.CreateScope();
                CharacterProgressionService service =
                    scope.ServiceProvider.GetRequiredService<CharacterProgressionService>();
                return await service.AddExperienceAsync(
                    session.UserId,
                    80,
                    CancellationToken.None);
            })
            .ToArray();
        await Task.WhenAll(changes);

        await using SqliteConnection connection = GameplayTestSupport.Open(application.DatabasePath);
        dynamic character = await connection.QuerySingleAsync(
            "SELECT level, exp FROM characters WHERE user_id = @UserId;",
            new { session.UserId });
        Assert.Equal(2L, (long)character.level);
        Assert.Equal(60L, (long)character.exp);
    }
}