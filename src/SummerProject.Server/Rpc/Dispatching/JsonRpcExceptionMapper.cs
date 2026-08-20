using SummerProject.Server.Exceptions.Auth;
using SummerProject.Server.Exceptions.Characters;
using SummerProject.Server.Exceptions.Currencies;
using SummerProject.Server.Exceptions.Stages;
using SummerProject.Server.Exceptions.Users;
using SummerProject.Server.Rpc.Contracts;
using SummerProject.Server.Rpc.Validation;

namespace SummerProject.Server.Rpc.Dispatching;

internal sealed class JsonRpcExceptionMapper
{
    public JsonRpcErrorPacket Map(Exception exception, string traceId)
    {
        if (exception is JsonRpcInvalidParamsException)
        {
            return JsonRpcErrors.InvalidParams(traceId);
        }

        return exception switch
        {
            InvalidGoogleTokenException => JsonRpcErrors.InvalidGoogleToken(traceId),
            InvalidRefreshTokenException => JsonRpcErrors.InvalidRefreshToken(traceId),
            RefreshTokenReusedException => JsonRpcErrors.RefreshTokenReused(traceId),
            DevelopmentUserNotFoundException => JsonRpcErrors.DevelopmentUserNotFound(traceId),
            UnauthenticatedCallerException => JsonRpcErrors.Unauthenticated(traceId),
            UserNotFoundException => JsonRpcErrors.UserNotFound(traceId),
            CharacterNotFoundException => JsonRpcErrors.CharacterNotFound(traceId),
            CharacterInvalidExperienceException => JsonRpcErrors.CharacterInvalidExperience(traceId),
            CurrencyInvalidTypeException => JsonRpcErrors.CurrencyInvalidType(traceId),
            CurrencyInsufficientException => JsonRpcErrors.CurrencyInsufficient(traceId),
            CurrencyInvalidAmountException => JsonRpcErrors.CurrencyInvalidAmount(traceId),
            CurrencyOverflowException => JsonRpcErrors.CurrencyOverflow(traceId),
            StageNotFoundException => JsonRpcErrors.StageNotFound(traceId),
            StageRunNotFoundException => JsonRpcErrors.StageRunNotFound(traceId),
            StageRunForbiddenException => JsonRpcErrors.StageRunForbidden(traceId),
            StageRunAlreadyCompletedException => JsonRpcErrors.StageRunAlreadyCompleted(traceId),
            StageClearTooEarlyException => JsonRpcErrors.StageClearTooEarly(traceId),
            StageRewardFailedException => JsonRpcErrors.StageRewardFailed(traceId),
            _ => JsonRpcErrors.InternalError(traceId)
        };
    }
}