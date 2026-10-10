using Core.Logging;
using Microsoft.Extensions.Logging;
using Tests.Shared.Logging;
using VlcLogLevel = LibVLCSharp.Shared.LogLevel;

namespace Core.Tests.Logging;

public class LibVlcLogForwarderTests
{
    private readonly TestLogger<LibVlcLogForwarder> logger = new TestLogger<LibVlcLogForwarder>();
    private readonly LibVlcLogForwarder forwarder;

    public LibVlcLogForwarderTests()
    {
        forwarder = new LibVlcLogForwarder(logger);
    }

    [Fact]
    public void Forward_Error_LogsErrorWithLibVlcPrefixAndModule()
    {
        forwarder.Forward(VlcLogLevel.Error, "filesystem", "cannot open file");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("(LibVLC) filesystem: cannot open file", entry.Message);
    }

    [Theory]
    [InlineData(VlcLogLevel.Warning)]
    [InlineData(VlcLogLevel.Notice)]
    public void Forward_WarningOrNotice_LogsDebug(VlcLogLevel level)
    {
        forwarder.Forward(level, "main", "buffer too late");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Equal("(LibVLC) main: buffer too late", entry.Message);
    }

    [Fact]
    public void Forward_Debug_DropsMessage()
    {
        forwarder.Forward(VlcLogLevel.Debug, "main", "creating demux");

        Assert.Empty(logger.Entries);
    }
}
