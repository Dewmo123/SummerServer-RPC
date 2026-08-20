using SummerProject.Server.Exceptions.Auth;
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
            _ => JsonRpcErrors.InternalError(traceId)
        };
    }
}