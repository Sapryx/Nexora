using System.Runtime.CompilerServices;
using Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Nexora.Logging;
using Tests.Shared.Logging;

namespace Nexora.Tests.Logging;

public class CrashLoggerTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-logs-{Guid.NewGuid()}");
    private readonly string logPath;

    public CrashLoggerTests()
    {
        Directory.CreateDirectory(directory);
        logPath = Path.Combine(directory, "session.log");
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void LogUnhandledException_AfterOtherEntries_FlushesAllOfThemWithStackTraceToFile()
    {
        var loggerFactory = LoggingInitializer.CreateLoggerFactory(logPath);
        var crashLogger = new CrashLogger(loggerFactory);
        loggerFactory.CreateLogger("Test").Info($"Before crash");

        crashLogger.LogUnhandledException(CreateThrownException());

        string[] lines = File.ReadAllLines(logPath);
        Assert.EndsWith("[Info] Before crash", lines[0]);
        Assert.EndsWith("[Crit] Unhandled exception, the application is terminating", lines[1]);
        Assert.Equal("System.InvalidOperationException: Boom", lines[2]);
        Assert.Contains(lines, it => it.Contains(nameof(CreateThrownException)));
    }

    [Fact]
    public void LogUnhandledException_Called_DisposesLoggerFactory()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(it => it.CreateLogger(It.IsAny<string>())).Returns(new TestLogger<CrashLogger>());
        var crashLogger = new CrashLogger(loggerFactoryMock.Object);

        crashLogger.LogUnhandledException(CreateThrownException());

        loggerFactoryMock.Verify(it => it.Dispose(), Times.Once);
    }

    [Fact]
    public void LogUiThreadException_Called_LogsCriticalWithExceptionWithoutDisposing()
    {
        var logger = new TestLogger<CrashLogger>();
        var loggerFactoryMock = CreateLoggerFactoryMock(logger);
        var crashLogger = new CrashLogger(loggerFactoryMock.Object);
        var exception = CreateThrownException();

        crashLogger.LogUiThreadException(exception);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Critical, entry.Level);
        Assert.Same(exception, entry.Exception);
        loggerFactoryMock.Verify(it => it.Dispose(), Times.Never);
    }

    [Fact]
    public void LogUnhandledException_SameExceptionAsOnUiThread_LogsItOnceAndDisposes()
    {
        var logger = new TestLogger<CrashLogger>();
        var loggerFactoryMock = CreateLoggerFactoryMock(logger);
        var crashLogger = new CrashLogger(loggerFactoryMock.Object);
        var exception = CreateThrownException();
        crashLogger.LogUiThreadException(exception);

        crashLogger.LogUnhandledException(exception);

        Assert.Single(logger.Entries);
        loggerFactoryMock.Verify(it => it.Dispose(), Times.Once);
    }

    [Fact]
    public void LogUnhandledException_DifferentExceptionThanOnUiThread_LogsBoth()
    {
        var logger = new TestLogger<CrashLogger>();
        var crashLogger = new CrashLogger(CreateLoggerFactoryMock(logger).Object);
        crashLogger.LogUiThreadException(CreateThrownException());
        var exception = CreateThrownException();

        crashLogger.LogUnhandledException(exception);

        Assert.Equal(2, logger.Entries.Count);
        Assert.Same(exception, logger.Entries[1].Exception);
    }

    [Fact]
    public void LogUnobservedTaskException_Called_LogsErrorWithException()
    {
        var logger = new TestLogger<CrashLogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(it => it.CreateLogger(It.IsAny<string>())).Returns(logger);
        var crashLogger = new CrashLogger(loggerFactoryMock.Object);
        var exception = new AggregateException(CreateThrownException());

        crashLogger.LogUnobservedTaskException(exception);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        loggerFactoryMock.Verify(it => it.Dispose(), Times.Never);
    }

    private static Mock<ILoggerFactory> CreateLoggerFactoryMock(TestLogger<CrashLogger> logger)
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock.Setup(it => it.CreateLogger(It.IsAny<string>())).Returns(logger);
        return loggerFactoryMock;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateThrownException()
    {
        try
        {
            throw new InvalidOperationException("Boom");
        }
        catch(InvalidOperationException ex)
        {
            return ex;
        }
    }
}
