using Core.Playback;
using Core.Playlists;
using Moq;
using Nexora.Media;
using Nexora.Threading;
using Nexora.ViewModels;

namespace Nexora.Tests.ViewModels;

public class PlaybackVmTests
{
    private const int InitialVolume = 50;
    private readonly Mock<IAudioPlayer> audioPlayerMock;
    private readonly Mock<IUiDispatcher> uiDispatcherMock;
    private readonly PlaybackVm vm;

    public PlaybackVmTests()
    {
        audioPlayerMock = new Mock<IAudioPlayer>();
        uiDispatcherMock = new Mock<IUiDispatcher>();

        audioPlayerMock
            .SetupGet(it => it.Volume)
            .Returns(InitialVolume);

        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => it());

        vm = new PlaybackVm(
            audioPlayerMock.Object,
            Mock.Of<ICoverCache>(),
            uiDispatcherMock.Object
        );
    }

    [Fact]
    public void Constructor_PlayerHasVolume_ShowsPlayerVolumeWithoutApplyingIt()
    {
        Assert.Equal(InitialVolume, vm.Volume);
        Assert.False(vm.IsMuted);
        audioPlayerMock.VerifySet(it => it.Volume = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void ToggleMute_NotMuted_MutesPlayerAndResetsVolumeToZero()
    {
        vm.ToggleMute();

        Assert.True(vm.IsMuted);
        Assert.Equal(0, vm.Volume);
        audioPlayerMock.VerifySet(it => it.Mute = true);
        audioPlayerMock.VerifySet(it => it.Volume = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void ToggleMute_Muted_UnmutesPlayerAndRestoresVolume()
    {
        vm.ToggleMute();

        vm.ToggleMute();

        Assert.False(vm.IsMuted);
        Assert.Equal(InitialVolume, vm.Volume);
        audioPlayerMock.VerifySet(it => it.Mute = false);
        audioPlayerMock.VerifySet(it => it.Volume = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void Volume_SetByUser_AppliesToPlayer()
    {
        vm.Volume = 70;

        audioPlayerMock.VerifySet(it => it.Volume = 70);
    }

    [Theory]
    [InlineData(0, "speaker_silent")]
    [InlineData(1, "speaker_low")]
    [InlineData(50, "speaker_low")]
    [InlineData(51, "speaker_loud")]
    [InlineData(100, "speaker_loud")]
    public void VolumeIconName_NotMuted_DependsOnVolume(int volume, string expected)
    {
        vm.Volume = volume;

        Assert.Equal(expected, vm.VolumeIconName);
    }

    [Fact]
    public void VolumeIconName_Muted_ReturnsMuteIcon()
    {
        vm.ToggleMute();

        Assert.Equal("speaker_mute", vm.VolumeIconName);
    }

    [Fact]
    public void Volume_Changed_NotifiesVolumeIconNameChanged()
    {
        List<string?> changedProperties = [];
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        vm.Volume = 80;

        Assert.Contains(nameof(PlaybackVm.VolumeIconName), changedProperties);
    }

    [Fact]
    public void ToggleMute_NotMuted_NotifiesVolumeIconNameChanged()
    {
        List<string?> changedProperties = [];
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

        vm.ToggleMute();

        Assert.Contains(nameof(PlaybackVm.VolumeIconName), changedProperties);
    }

    [Fact]
    public void Volume_IncreasedByUserWhileMuted_UnmutesWithNewVolume()
    {
        vm.ToggleMute();

        vm.Volume = 30;

        Assert.False(vm.IsMuted);
        Assert.Equal(30, vm.Volume);
        audioPlayerMock.VerifySet(it => it.Volume = 30);
        audioPlayerMock.VerifySet(it => it.Mute = false);
    }

    [Fact]
    public void ToggleMute_AfterUnmuteBySlider_RestoresSliderVolume()
    {
        vm.ToggleMute();
        vm.Volume = 30;

        vm.ToggleMute();
        vm.ToggleMute();

        Assert.Equal(30, vm.Volume);
    }

    [Fact]
    public void MuteChanged_UnmutedByPlayerWhileDraggingVolume_KeepsDraggedVolume()
    {
        vm.ToggleMute();
        vm.IsDraggingVolume = true;
        vm.Volume = 30;

        audioPlayerMock.Raise(it => it.MuteChanged += null, false);

        Assert.False(vm.IsMuted);
        Assert.Equal(30, vm.Volume);
    }

    [Fact]
    public void MuteChanged_MutedByPlayer_ResetsVolumeToZero()
    {
        audioPlayerMock.Raise(it => it.MuteChanged += null, true);

        Assert.True(vm.IsMuted);
        Assert.Equal(0, vm.Volume);
        audioPlayerMock.VerifySet(it => it.Volume = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void MuteChanged_UnmutedByPlayer_RestoresVolume()
    {
        audioPlayerMock.Raise(it => it.MuteChanged += null, true);

        audioPlayerMock.Raise(it => it.MuteChanged += null, false);

        Assert.False(vm.IsMuted);
        Assert.Equal(InitialVolume, vm.Volume);
    }

    [Fact]
    public void MuteChanged_ConfirmsMuteAlreadyApplied_KeepsVolumeToRestore()
    {
        vm.ToggleMute();

        audioPlayerMock.Raise(it => it.MuteChanged += null, true);
        vm.ToggleMute();

        Assert.Equal(InitialVolume, vm.Volume);
    }

    [Fact]
    public void MuteChanged_Raised_IsAppliedOnlyThroughUiDispatcher()
    {
        List<Action> postedActions = [];
        uiDispatcherMock
            .Setup(it => it.Post(It.IsAny<Action>()))
            .Callback<Action>(it => postedActions.Add(it));

        audioPlayerMock.Raise(it => it.MuteChanged += null, true);

        Assert.False(vm.IsMuted);

        postedActions.ForEach(it => it());

        Assert.True(vm.IsMuted);
    }

    [Fact]
    public void VolumeChanged_NotMuted_UpdatesVolumeWithoutApplyingToPlayer()
    {
        audioPlayerMock.Raise(it => it.VolumeChanged += null, 42f);

        Assert.Equal(42, vm.Volume);
        audioPlayerMock.VerifySet(it => it.Volume = It.IsAny<int>(), Times.Never);
    }

    [Fact]
    public void VolumeChanged_FractionalValue_RoundsToNearest()
    {
        audioPlayerMock.Raise(it => it.VolumeChanged += null, 36.99f);

        Assert.Equal(37, vm.Volume);
    }

    [Fact]
    public void VolumeChanged_WhileMuted_KeepsZeroAndRestoresNewVolumeOnUnmute()
    {
        vm.ToggleMute();

        audioPlayerMock.Raise(it => it.VolumeChanged += null, 20f);

        Assert.Equal(0, vm.Volume);

        vm.ToggleMute();

        Assert.Equal(20, vm.Volume);
    }

    [Fact]
    public void VolumeChanged_WhileDraggingVolume_DoesNotMoveSlider()
    {
        vm.IsDraggingVolume = true;
        vm.Volume = 70;

        audioPlayerMock.Raise(it => it.VolumeChanged += null, 65f);

        Assert.Equal(70, vm.Volume);
    }

    [Fact]
    public void PlaybackPosition_SetByUser_SeeksPlayer()
    {
        vm.PlaybackPosition = 0.5f;

        audioPlayerMock.VerifySet(it => it.PlaybackPosition = 0.5f);
    }

    [Fact]
    public void PlaybackPositionChanged_NotDragging_UpdatesPositionWithoutSeeking()
    {
        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.25f);

        Assert.Equal(0.25f, vm.PlaybackPosition);
        audioPlayerMock.VerifySet(it => it.PlaybackPosition = It.IsAny<float>(), Times.Never);
    }

    [Fact]
    public void PlaybackPositionChanged_WhileDraggingPosition_DoesNotMoveSlider()
    {
        vm.IsDraggingPosition = true;
        vm.PlaybackPosition = 0.8f;

        audioPlayerMock.Raise(it => it.PlaybackPositionChanged += null, 0.3f);

        Assert.Equal(0.8f, vm.PlaybackPosition);
    }

    [Fact]
    public void PreviewSeek_TrackPlaying_ShowsTimeAtPosition()
    {
        SetupNowPlaying(TimeSpan.FromSeconds(200));

        vm.PreviewSeek(0.5);

        Assert.Equal("01:40", vm.SeekPreviewTime);
    }

    [Fact]
    public void PreviewSeek_PositionOutOfRange_ClampsToTrackBounds()
    {
        SetupNowPlaying(TimeSpan.FromSeconds(200));

        vm.PreviewSeek(1.5);

        Assert.Equal("03:20", vm.SeekPreviewTime);
    }

    [Fact]
    public void PreviewSeek_NothingPlaying_HidesPreview()
    {
        vm.PreviewSeek(0.5);

        Assert.Null(vm.SeekPreviewTime);
    }

    private void SetupNowPlaying(TimeSpan duration)
    {
        var metadata = new Metadata() { Duration = duration };
        var playlistItem = Mock.Of<IPlaylistItem>(it => it.AudioTrack.Metadata == metadata);

        audioPlayerMock
            .SetupGet(it => it.NowPlaying)
            .Returns(playlistItem);
    }
}
