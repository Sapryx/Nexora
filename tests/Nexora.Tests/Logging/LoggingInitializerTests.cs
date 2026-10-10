using System.Text;
using System.Text.RegularExpressions;
using Core.Logging;
using Microsoft.Extensions.Logging;
using Nexora.Logging;
using ZLogger;

namespace Nexora.Tests.Logging;

public class LoggingInitializerTests : IDisposable
{
    private const string ResetColor = "\u001b[0m";
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-logs-{Guid.NewGuid()}");
    private readonly string logPath;

    public LoggingInitializerTests()
    {
        Directory.CreateDirectory(directory);
        logPath = Path.Combine(directory, "session.log");
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void CreateLoggerFactory_MessageLogged_WritesTimeWithMillisecondsAndLevel()
    {
        using(var loggerFactory = LoggingInitializer.CreateLoggerFactory(logPath))
        {
            loggerFactory.CreateLogger("Test").Warn($"Hello");
        }

        Assert.Matches(@"^\d{2}:\d{2}:\d{2}\.\d{3} \[Warn\] Hello$", File.ReadAllLines(logPath).Single());
    }

    [Fact]
    public void CreateLoggerFactory_MessageLogged_IsReadableFromFileWhileFactoryIsAlive()
    {
        using var loggerFactory = LoggingInitializer.CreateLoggerFactory(logPath);

        loggerFactory.CreateLogger("Test").Info($"Running");

        Assert.True(SpinWait.SpinUntil(() => ReadShared(logPath).Contains("[Info] Running"), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void CreateLoggerFactory_DiscordRpcMessagesBelowInfo_AreNotWritten()
    {
        using(var loggerFactory = LoggingInitializer.CreateLoggerFactory(logPath))
        {
            var rpcLogger = new DiscordRpcLogger(loggerFactory.CreateLogger<DiscordRpcLogger>());
            rpcLogger.Error("Failed connection to discord-ipc-0.");
            loggerFactory.CreateLogger("Test").Info($"After Discord");
        }

        Assert.EndsWith("[Info] After Discord", File.ReadAllLines(logPath).Single());
    }

    [Theory]
    [InlineData(LogLevel.Debug, "\u001b[90m")]
    [InlineData(LogLevel.Warning, "\u001b[33m")]
    [InlineData(LogLevel.Error, "\u001b[31m")]
    [InlineData(LogLevel.Critical, "\u001b[97;41m")]
    public void UseColoredFormatter_MessageLogged_ColorsWholeLine(LogLevel level, string color)
    {
        string output = LogColored(logger => logger.Log(level, "(LibVLC) main: Hello"));

        Assert.Matches($@"^{Regex.Escape(color)}\d{{2}}:\d{{2}}:\d{{2}}\.\d{{3}} \[\w+\] \(LibVLC\) main: Hello{Regex.Escape(ResetColor)}\r?\n$", output);
    }

    [Fact]
    public void UseColoredFormatter_InfoMessage_KeepsDefaultColor()
    {
        string output = LogColored(logger => logger.Info($"Hello"));

        Assert.Matches($@"^\d{{2}}:\d{{2}}:\d{{2}}\.\d{{3}} \[Info\] Hello{Regex.Escape(ResetColor)}\r?\n$", output);
    }

    [Fact]
    public void UseColoredFormatter_MessageWithException_ColorsExceptionAndResetsAfterIt()
    {
        string output = LogColored(logger => logger.Error(new InvalidOperationException("Boom"), $"Failed"));

        string[] lines = output.TrimEnd().Split(Environment.NewLine);
        Assert.Matches(@"^\u001b\[31m\d{2}:\d{2}:\d{2}\.\d{3} \[Error\] Failed$", lines[0]);
        Assert.Equal($"System.InvalidOperationException: Boom{ResetColor}", lines[1]);
    }

    [Fact]
    public void CreateLoggerFactory_MessageLogged_WritesNoColorCodesToFile()
    {
        using(var loggerFactory = LoggingInitializer.CreateLoggerFactory(logPath))
        {
            loggerFactory.CreateLogger("Test").Error($"Hello");
        }

        Assert.DoesNotContain('\u001b', File.ReadAllText(logPath));
    }

    private static string LogColored(Action<ILogger> log)
    {
        var stream = new MemoryStream();

        using(var loggerFactory = LoggerFactory.Create(it => it.SetMinimumLevel(LogLevel.Trace).AddZLoggerStream(stream, LoggingInitializer.UseColoredFormatter)))
        {
            log(loggerFactory.CreateLogger("Test"));
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
