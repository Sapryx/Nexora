using Core.Logging;
using Nexora.Logging;

namespace Nexora.Tests.Logging;

public class LoggingInitializerTests : IDisposable
{
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

    private static string ReadShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
