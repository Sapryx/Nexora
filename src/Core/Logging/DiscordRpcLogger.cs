using System.Globalization;
using Microsoft.Extensions.Logging;
using IDiscordLogger = DiscordRPC.Logging.ILogger;
using DiscordLogLevel = DiscordRPC.Logging.LogLevel;

namespace Core.Logging;

public class DiscordRpcLogger : IDiscordLogger
{
    private readonly ILogger<DiscordRpcLogger> logger;

    public DiscordLogLevel Level { get; set; } = DiscordLogLevel.Info;

    public DiscordRpcLogger(ILogger<DiscordRpcLogger> logger)
    {
        this.logger = logger;
    }

    public void Trace(string message, params object[] args)
    {
        if(Level <= DiscordLogLevel.Trace)
        {
            logger.Trace($"(Discord) {Format(message, args)}");
        }
    }

    public void Info(string message, params object[] args)
    {
        if(Level <= DiscordLogLevel.Info)
        {
            logger.Trace($"(Discord) {Format(message, args)}");
        }
    }

    public void Warning(string message, params object[] args)
    {
        if(Level <= DiscordLogLevel.Warning)
        {
            logger.Trace($"(Discord) {Format(message, args)}");
        }
    }

    public void Error(string message, params object[] args)
    {
        if(Level <= DiscordLogLevel.Error)
        {
            logger.Debug($"(Discord) {Format(message, args)}");
        }
    }

    private static string Format(string message, object[] args)
    {
        return args.Length == 0
            ? message
            : string.Format(CultureInfo.InvariantCulture, message, args);
    }
}
