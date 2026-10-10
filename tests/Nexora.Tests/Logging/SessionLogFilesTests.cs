using Microsoft.Extensions.Logging.Abstractions;
using Nexora.Logging;

namespace Nexora.Tests.Logging;

public class SessionLogFilesTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-logs-{Guid.NewGuid()}");

    public SessionLogFilesTests()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void GetPath_StartTime_ReturnsSessionFileNamedByTime()
    {
        string path = SessionLogFiles.GetPath(directory, new DateTime(2026, 10, 10, 17, 5, 7));

        Assert.Equal(Path.Combine(directory, "session-2026-10-10_17-05-07.log"), path);
    }

    [Fact]
    public void DeleteOld_MoreLogsThanKept_KeepsCurrentAndNewestOnes()
    {
        var oldLogs = Enumerable.Range(1, 12).Select(day => CreateLog(new DateTime(2026, 9, day))).ToList();
        string currentLog = CreateLog(new DateTime(2026, 10, 10));

        SessionLogFiles.DeleteOld(currentLog, 10, NullLogger.Instance);

        string[] expected = oldLogs.Skip(3).Append(currentLog).Order().ToArray();
        Assert.Equal(expected, Directory.GetFiles(directory).Order());
    }

    [Fact]
    public void DeleteOld_CurrentLogNotCreatedYet_StillKeepsRoomForIt()
    {
        var oldLogs = Enumerable.Range(1, 10).Select(day => CreateLog(new DateTime(2026, 9, day))).ToList();
        string currentLog = SessionLogFiles.GetPath(directory, new DateTime(2026, 10, 10));

        SessionLogFiles.DeleteOld(currentLog, 10, NullLogger.Instance);

        Assert.Equal(oldLogs.Skip(1).Order(), Directory.GetFiles(directory).Order());
    }

    [Fact]
    public void DeleteOld_FewerLogsThanKept_DeletesNothing()
    {
        var logs = Enumerable.Range(1, 5).Select(day => CreateLog(new DateTime(2026, 9, day))).ToList();

        SessionLogFiles.DeleteOld(logs.Last(), 10, NullLogger.Instance);

        Assert.Equal(logs.Order(), Directory.GetFiles(directory).Order());
    }

    [Fact]
    public void DeleteOld_OtherFilesInDirectory_KeepsThem()
    {
        string[] otherFiles =
        [
            CreateFile("session.log"),
            CreateFile("crash-2026-09-01_10-00-00.log"),
            CreateFile("discord.log")
        ];
        string currentLog = CreateLog(new DateTime(2026, 10, 10));

        SessionLogFiles.DeleteOld(currentLog, 1, NullLogger.Instance);

        Assert.Equal(otherFiles.Append(currentLog).Order(), Directory.GetFiles(directory).Order());
    }

    private string CreateLog(DateTime startTime)
    {
        string path = SessionLogFiles.GetPath(directory, startTime);
        File.WriteAllText(path, "");
        return path;
    }

    private string CreateFile(string name)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllText(path, "");
        return path;
    }
}
