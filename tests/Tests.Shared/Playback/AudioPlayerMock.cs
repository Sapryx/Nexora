using Core.Playback;
using Moq;

namespace Tests.Shared.Playback;

public static class AudioPlayerMock
{
    public static Mock<IAudioPlayer> Create()
    {
        var audioPlayerMock = new Mock<IAudioPlayer>();
        audioPlayerMock.SetupGet(it => it.State).Returns(PlaybackState.Stopped);
        audioPlayerMock
            .Setup(it => it.ReleaseFileAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((audioPath, action) => action());

        return audioPlayerMock;
    }
}
