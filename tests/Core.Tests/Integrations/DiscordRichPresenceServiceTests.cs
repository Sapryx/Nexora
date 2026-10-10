using Core.Integrations;
using Core.Playback;
using Moq;

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
    public void UpdateStatus_TextLongerThanLimit_DoesNotThrow()
    {
        using var service = new DiscordRichPresenceService(Mock.Of<IAudioPlayer>());
        string text = new string('a', 300);

        var exception = Record.Exception(() => service.UpdateStatus(text, text));

        Assert.Null(exception);
    }
}
