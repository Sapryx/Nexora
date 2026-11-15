using Core.Integrations;
using Core.Library;
using Core.Playback;
using Core.Playlists;
using DiscordRPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Tests.Shared.Logging;
using Tests.Shared.Playback;

namespace Core.Tests.Integrations;

public class DiscordRichPresenceServiceTests
{
    private readonly Mock<IAudioPlayer> audioPlayerMock = AudioPlayerMock.Create();
    private readonly Mock<IDiscordClient> discordClientMock = new Mock<IDiscordClient>();

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
        var service = CreateService(logger);

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
        var service = CreateService(logger);
        service.OnConnectionFailed();
        service.OnReady("user");

        service.OnConnectionFailed();

        Assert.Equal(2, logger.Entries.Count(it => it.Message.Contains("not running")));
    }

    [Fact]
    public void OnReady_Called_LogsInfoWithUsername()
    {
        var logger = new TestLogger<DiscordRichPresenceService>();
        var service = CreateService(logger);

        service.OnReady("user");

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("(Discord) Connected as user", entry.Message);
    }

    [Fact]
    public void UpdateStatus_TextLongerThanLimit_DoesNotThrow()
    {
        var service = CreateService(NullLogger<DiscordRichPresenceService>.Instance);
        string text = new string('a', 300);

        var exception = Record.Exception(() => service.UpdateStatus(text, text));

        Assert.Null(exception);
    }

    [Fact]
    public void StateChanged_PlayingTrack_ShowsTrackInPresence()
    {
        var service = CreateService(NullLogger<DiscordRichPresenceService>.Instance);

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(CreatePlaylistItem()));

        discordClientMock.Verify(it => it.SetPresence(
            It.Is<RichPresence?>(presence =>
                presence != null && presence.Details == "Psycho" && presence.State == "Muse")
        ));
    }

    [Fact]
    public void StateChanged_Stopped_ClearsPresence()
    {
        var service = CreateService(NullLogger<DiscordRichPresenceService>.Instance);
        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Playing(CreatePlaylistItem()));

        audioPlayerMock.Raise(it => it.StateChanged += null, PlaybackState.Stopped);

        discordClientMock.Verify(it => it.SetPresence(null));
    }

    private DiscordRichPresenceService CreateService(ILogger<DiscordRichPresenceService> logger)
    {
        return new DiscordRichPresenceService(audioPlayerMock.Object, discordClientMock.Object, logger);
    }

    private static IPlaylistItem CreatePlaylistItem()
    {
        var metadata = new Metadata() { Title = "Psycho", Artists = ["Muse"] };

        return Mock.Of<IPlaylistItem>(it => it.AudioTrack.Metadata == metadata);
    }
}
