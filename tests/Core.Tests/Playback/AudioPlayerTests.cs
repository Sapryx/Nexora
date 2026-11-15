using Core.Library;
using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Shared.Logging;

namespace Core.Tests.Playback;

public class AudioPlayerTests
{
    private readonly TestLogger<AudioPlayer> logger = new TestLogger<AudioPlayer>();
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();
    private readonly Mock<IVolumeStorage> volumeStorageMock = new Mock<IVolumeStorage>();
    private readonly Playlist playlist = new Playlist();
    private readonly List<PlaybackState> states = [];

    [Fact]
    public void PlayTrack_Stopped_OpensTrackAndReportsPlaying()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");

        player.PlayTrack(item);

        Assert.Equal("/music/one.opus", mediaEngine.OpenedPath);
        Assert.False(mediaEngine.IsPaused);
        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
        Assert.Same(item, player.State.Track);
        Assert.Equal([player.State], states);
    }

    [Fact]
    public void TogglePause_Playing_PausesTrackAndReportsPaused()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        player.PlayTrack(item);

        player.TogglePause();

        Assert.True(mediaEngine.IsPaused);
        Assert.Equal(PlaybackStatus.Paused, player.State.Status);
        Assert.Same(item, player.State.Track);
        Assert.Same(player.State, states.Last());
    }

    [Fact]
    public void Stop_Playing_StopsTrackAndReportsStopped()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));

        player.Stop();

        Assert.True(mediaEngine.IsStopped);
        Assert.Same(PlaybackState.Stopped, player.State);
        Assert.Same(PlaybackState.Stopped, states.Last());
    }

    [Fact]
    public void EndReached_TrackHasNext_PlaysNextTrack()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        var next = AddTrack("/music/two.opus");

        mediaEngine.RaiseEndReached();

        Assert.Equal("/music/two.opus", mediaEngine.OpenedPath);
        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
        Assert.Same(next, player.State.Track);
        Assert.Same(player.State, states.Last());
    }

    [Fact]
    public void EndReached_LastTrack_ReportsPausedOnLastTrack()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        player.PlayTrack(item);

        mediaEngine.RaiseEndReached();

        Assert.Equal(PlaybackStatus.Paused, player.State.Status);
        Assert.Same(item, player.State.Track);
        Assert.Same(player.State, states.Last());
    }

    [Fact]
    public void TogglePause_PausedAtEndOfLastTrack_PlaysItFromStart()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        player.PlayTrack(item);
        mediaEngine.RaiseEndReached();

        player.TogglePause();

        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
        Assert.Same(item, player.State.Track);
    }

    [Fact]
    public void EncounteredError_Playing_LogsErrorAndReportsStopped()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/broken.opus"));

        mediaEngine.RaiseEncounteredError();

        var error = Assert.Single(logger.Entries, it => it.Level == LogLevel.Error);
        Assert.Contains("/music/broken.opus", error.Message);
        Assert.True(mediaEngine.IsStopped);
        Assert.Same(PlaybackState.Stopped, player.State);
        Assert.Same(PlaybackState.Stopped, states.Last());
    }

    [Fact]
    public async Task ReleaseFileAsync_Playing_ClosesFileDuringActionAndReopensItAtSameTime()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.Time = 4200;
        states.Clear();
        bool wasStoppedDuringAction = false;

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            wasStoppedDuringAction = mediaEngine.IsStopped;
            return Task.CompletedTask;
        });

        Assert.True(wasStoppedDuringAction);
        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(4200, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
        Assert.Empty(states);
    }

    [Fact]
    public async Task ReleaseFileAsync_PausedDuringAction_ReportsPausedAndReopensPaused()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        player.PlayTrack(item);

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.TogglePause();
            return Task.CompletedTask;
        });

        Assert.Equal(PlaybackStatus.Paused, player.State.Status);
        Assert.Same(player.State, states.Last());
        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.True(mediaEngine.IsPaused);
    }

    [Fact]
    public async Task ReleaseFileAsync_FileNotHeld_RunsActionWithoutReopening()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        bool actionRan = false;

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            actionRan = true;
            return Task.CompletedTask;
        });

        Assert.True(actionRan);
        Assert.Equal(1, mediaEngine.OpenCount);
    }

    [Fact]
    public async Task ReleaseFileAsync_OtherTrackPlayedDuringAction_KeepsOtherTrack()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        var other = AddTrack("/music/two.opus");

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.PlayTrack(other);
            return Task.CompletedTask;
        });

        Assert.Equal("/music/two.opus", mediaEngine.OpenedPath);
        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Same(other, player.State.Track);
    }

    [Fact]
    public async Task ReleaseFileAsync_OtherFile_RunsActionWithoutReleasing()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        bool wasStoppedDuringAction = true;

        await player.ReleaseFileAsync("/music/two.opus", () =>
        {
            wasStoppedDuringAction = mediaEngine.IsStopped;
            return Task.CompletedTask;
        });

        Assert.False(wasStoppedDuringAction);
        Assert.Equal(1, mediaEngine.OpenCount);
    }

    [Fact]
    public async Task ReleaseFileAsync_OverlappingReleases_ReopensOnceAfterLast()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.Time = 4200;
        var firstAction = new TaskCompletionSource();
        var secondAction = new TaskCompletionSource();

        var first = player.ReleaseFileAsync("/music/one.opus", () => firstAction.Task);
        var second = player.ReleaseFileAsync("/music/one.opus", () => secondAction.Task);
        firstAction.SetResult();
        await first;

        Assert.True(mediaEngine.IsStopped);
        Assert.Equal(1, mediaEngine.OpenCount);

        secondAction.SetResult();
        await second;

        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(4200, mediaEngine.OpenedStartTimeMs);
    }

    [Fact]
    public void Constructor_NoSavedVolume_AppliesDefaultVolume()
    {
        var player = CreatePlayer();

        Assert.Equal(50, player.Volume);
        Assert.Equal(50, mediaEngine.Volume);
    }

    [Fact]
    public void VolumeChanged_EngineReportsNewVolume_SavesRoundedVolume()
    {
        var player = CreatePlayer();

        mediaEngine.RaiseVolumeChanged(72.6f);

        Assert.Equal(73, player.Volume);
        volumeStorageMock.Verify(it => it.Save(73));
    }

    [Fact]
    public void VolumeChanged_EngineReportsNegativeVolume_KeepsVolume()
    {
        var player = CreatePlayer();

        mediaEngine.RaiseVolumeChanged(-1);

        Assert.Equal(50, player.Volume);
        volumeStorageMock.Verify(it => it.Save(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void PlayTrack_TrackHasLoudness_AppliesGainTowardsTarget()
    {
        var player = CreatePlayer();
        playlist.AddTrack(new AudioTrack(
            "/music/loud.opus",
            new Metadata() { Loudness = new TrackLoudness() { Lufs = -8 } }
        ));

        player.PlayTrack(playlist.GetItem(0));

        Assert.Equal(-6, mediaEngine.OpenedGainDb, 2);
    }

    [Fact]
    public void SkipForward_Playing_MovesFiveSecondsAhead()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.Position = 0.5f;

        player.SkipForward();

        Assert.Equal(0.55f, mediaEngine.Position, 4);
    }

    private AudioPlayer CreatePlayer()
    {
        var player = new AudioPlayer(logger, mediaEngine, volumeStorageMock.Object);
        player.StateChanged += it => states.Add(it);

        return player;
    }

    private IPlaylistItem AddTrack(string audioPath)
    {
        playlist.AddTrack(new AudioTrack(audioPath, new Metadata() { Duration = TimeSpan.FromSeconds(100) }));

        return playlist.GetItem(playlist.TrackCount - 1);
    }
}
