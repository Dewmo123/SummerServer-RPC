using System.Numerics;

using SummerProject.Server.Models.Datas.Characters;

namespace SummerProject.Server.Helpers.Characters;

internal sealed class CharacterProgressionCalculator
{
    private const long ExperiencePerLevel = 100;

    public bool TryAddExperience(
        CharacterModel current,
        long amount,
        out CharacterModel? updated)
    {
        updated = null;
        if (current.Level < 1 || current.Exp < 0 || amount <= 0)
        {
            return false;
        }

        // 여러 레벨 누적 계산 중 Int64가 먼저 넘치지 않도록 중간값은 임의 정밀도로 계산한다.
        BigInteger totalExperience = (BigInteger)current.Exp + amount;
        long gainedLevels = FindGainedLevels(current.Level, totalExperience);
        BigInteger finalLevel = (BigInteger)current.Level + gainedLevels;
        BigInteger remainingExperience = totalExperience
            - RequiredExperience(current.Level, gainedLevels);
        BigInteger nextLevelRequirement = finalLevel * ExperiencePerLevel;

        if (finalLevel > long.MaxValue
            || remainingExperience > long.MaxValue
            || nextLevelRequirement > long.MaxValue
            || remainingExperience >= nextLevelRequirement)
        {
            return false;
        }

        updated = new CharacterModel
        {
            UserId = current.UserId,
            Level = (long)finalLevel,
            Exp = (long)remainingExperience
        };
        return true;
    }

    public bool TryGetNextLevelRequirement(long level, out long requirement)
    {
        BigInteger value = (BigInteger)level * ExperiencePerLevel;
        if (level < 1 || value > long.MaxValue)
        {
            requirement = 0;
            return false;
        }

        requirement = (long)value;
        return true;
    }

    private static long FindGainedLevels(long currentLevel, BigInteger availableExperience)
    {
        long lower = 0;
        long upper = 1;

        while (RequiredExperience(currentLevel, upper) <= availableExperience)
        {
            lower = upper;
            if (upper > long.MaxValue / 2)
            {
                return upper;
            }

            upper *= 2;
        }

        while (lower + 1 < upper)
        {
            long middle = lower + ((upper - lower) / 2);
            if (RequiredExperience(currentLevel, middle) <= availableExperience)
            {
                lower = middle;
            }
            else
            {
                upper = middle;
            }
        }

        return lower;
    }

    private static BigInteger RequiredExperience(long currentLevel, long gainedLevels)
    {
        BigInteger levels = gainedLevels;
        return 50 * levels * (((BigInteger)2 * currentLevel) + levels - 1);
    }
}