using Core.Integrations;
using Core.Logging;
using Core.Playback;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Shared.Logging;

namespace Core.Tests.Integrations;

public class DiscordRichPresenceServiceTests
{
    [Fact]
    public void CreatePresence_TextWithinLimit_KeepsItAsIs()
    {
        var presence = DiscordRichPresenceService.CreatePresence("Title", "Artist");

        Assert.Equal("Title", presence.Details);
        Assert.Equal("Artist", presence.State);
    }

    [Fact]
    public void CreatePresence_TextOfExactlyLimit_KeepsItAsIs()
    {
        string text = new string('a', 128);

        var presence = DiscordRichPresenceService.CreatePresence(text, text);

        Assert.Equal(text, presence.Details);
        Assert.Equal(text, presence.State);
    }

    [Fact]
    public void CreatePresence_TextLongerThanLimit_TruncatesToLimitWithEllipsis()
    {
        string text = new string('ж', 300);

        var presence = DiscordRichPresenceService.CreatePresence(text, text);

        Assert.Equal(new string('ж', 127) + "…", presence.Details);
        Assert.Equal(new string('ж', 127) + "…", presence.State);
    }

    [Fact]
    public void CreatePresence_SurrogatePairCrossesLimit_DoesNotSplitIt()
    {
        string text = new string('a', 126) + "😀😀";

        var presence = DiscordRichPresenceService.CreatePresence(text, "Artist");

        Assert.Equal(new string('a', 126) + "…", presence.Details);
    }

    [Fact]
    public void OnConnectionFailed_CalledRepeatedly_LogsInfoOnce()
    {
        var logger = new TestLogger<DiscordRichPresenceService>();
        using var service = new DiscordRichPresenceService(Mock.Of<IAudioPlayer>(), logger, NullLogger<DiscordRpcLogger>.Instance);

        service.OnConnectionFailed();
        service.OnConnectionFailed();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.StartsWith("(Discord)", entry.Message);
    }

    [Fact]
    public void OnConnectionFailed_AfterReady_LogsAgain()
    {
        var logger = new TestLogger<DiscordRichPresenceService>();
        using var service = new DiscordRichPresenceService(Mock.Of<IAudioPlayer>(), logger, NullLogger<DiscordRpcLogger>.Instance);
        service.OnConnectionFailed();
        service.OnReady("user");

        service.OnConnectionFailed();

        Assert.Equal(2, logger.Entries.Count(it => it.Message.Contains("not running")));
    }

    [Fact]
    public void OnReady_Called_LogsInfoWithUsername()
    {
        var logger = new TestLogger<DiscordRichPresenceService>();
        using var service = new DiscordRichPresenceService(Mock.Of<IAudioPlayer>(), logger, NullLogger<DiscordRpcLogger>.Instance);

        service.OnReady("user");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("(Discord) Connected as user", entry.Message);
    }

    [Fact]
    public void UpdateStatus_TextLongerThanLimit_DoesNotThrow()
    {
        using var service = new DiscordRichPresenceService(Mock.Of<IAudioPlayer>(), NullLogger<DiscordRichPresenceService>.Instance, NullLogger<DiscordRpcLogger>.Instance);
        string text = new string('a', 300);

        var exception = Record.Exception(() => service.UpdateStatus(text, text));

        Assert.Null(exception);
    }
}
