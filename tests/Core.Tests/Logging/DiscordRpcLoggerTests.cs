using Core.Logging;
using Microsoft.Extensions.Logging;
using Tests.Shared.Logging;
using DiscordLogLevel = DiscordRPC.Logging.LogLevel;

namespace Core.Tests.Logging;

public class DiscordRpcLoggerTests
{
    private readonly TestLogger<DiscordRpcLogger> logger = new TestLogger<DiscordRpcLogger>();
    private readonly DiscordRpcLogger discordLogger;

    public DiscordRpcLoggerTests()
    {
        discordLogger = new DiscordRpcLogger(logger);
    }

    [Fact]
    public void Trace_DefaultLevel_DropsMessage()
    {
        discordLogger.Trace("Assembly: DiscordRPC");
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Trace_LevelIsTrace_LogsTraceWithDiscordPrefix()
    {
        discordLogger.Level = DiscordLogLevel.Trace;
        discordLogger.Trace("Assembly: DiscordRPC");
        AssertLogged(LogLevel.Trace, "(Discord) Assembly: DiscordRPC");
    }

    [Fact]
    public void Info_Called_LogsTrace()
    {
        discordLogger.Info("Attempting a new connection");
        AssertLogged(LogLevel.Trace, "(Discord) Attempting a new connection");
    }

    [Fact]
    public void Warning_Called_LogsTrace()
    {
        discordLogger.Warning("Tried to close a already closed pipe.");
        AssertLogged(LogLevel.Trace, "(Discord) Tried to close a already closed pipe.");
    }

    [Fact]
    public void Error_Called_LogsDebug()
    {
        discordLogger.Error("Failed to connect for some reason.");
        AssertLogged(LogLevel.Debug, "(Discord) Failed to connect for some reason.");
    }

    [Fact]
    public void Info_LevelIsError_DropsMessage()
    {
        discordLogger.Level = DiscordLogLevel.Error;
        discordLogger.Info("Attempting a new connection");
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Info_MessageWithArguments_FormatsThem()
    {
        discordLogger.Info("Attempting to connect to '{0}'", "discord-ipc-0");
        AssertLogged(LogLevel.Trace, "(Discord) Attempting to connect to 'discord-ipc-0'");
    }

    [Fact]
    public void Info_MessageWithBracesAndNoArguments_KeepsItAsIs()
    {
        discordLogger.Info("Payload {\"cmd\":\"SET_ACTIVITY\"}");
        AssertLogged(LogLevel.Trace, "(Discord) Payload {\"cmd\":\"SET_ACTIVITY\"}");
    }

    private void AssertLogged(LogLevel level, string message)
    {
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(level, entry.Level);
        Assert.Equal(message, entry.Message);
    }
}
