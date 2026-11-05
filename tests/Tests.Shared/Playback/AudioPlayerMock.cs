using Core.Playback;
using Moq;

namespace Tests.Shared.Playback;

public static class AudioPlayerMock
{
    public static Mock<IAudioPlayer> Create()
    {
        var audioPlayerMock = new Mock<IAudioPlayer>();
        audioPlayerMock.SetupGet(it => it.State).Returns(PlaybackState.Stopped);

        return audioPlayerMock;
    }
}
