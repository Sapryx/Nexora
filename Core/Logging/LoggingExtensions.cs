using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Core.Logging;

public static class LoggingExtensions
{
    public static void Trace(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerTraceInterpolatedStringHandler message)
    {
        logger.ZLogTrace(ref message);
    }

    public static void Debug(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerDebugInterpolatedStringHandler message)
    {
        logger.ZLogDebug(ref message);
    }

    public static void Info(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerInformationInterpolatedStringHandler message)
    {
        logger.ZLogInformation(ref message);
    }

    public static void Warn(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerWarningInterpolatedStringHandler message)
    {
        logger.ZLogWarning(ref message);
    }

    public static void Error(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerErrorInterpolatedStringHandler message)
    {
        logger.ZLogError(ref message);
    }

    public static void Crit(
        this ILogger logger,
        [InterpolatedStringHandlerArgument("logger")] ref ZLoggerCriticalInterpolatedStringHandler message)
    {
        logger.ZLogCritical(ref message);
    }
}
