using Core.Playback;
using Core.Playlists;
using Core.Storage;
using Core.Tracks;
using Microsoft.Extensions.Logging;
using Moq;
using Tests.Shared.Logging;
using Tests.Shared.Playback;

namespace Core.Tests.Playback;

public class AudioPlayerTests
{
    private readonly TestLogger<AudioPlayer> logger = new TestLogger<AudioPlayer>();
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();
    private readonly Mock<IVolumeStorage> volumeStorageMock = new Mock<IVolumeStorage>();
    private readonly Playlist playlist = new Playlist();
    private readonly List<PlaybackState> states = [];
    private readonly List<float> positions = [];

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
    public void EndReached_Stopped_StaysStopped()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        player.Stop();
        states.Clear();

        mediaEngine.RaiseEndReached();

        Assert.Same(PlaybackState.Stopped, player.State);
        Assert.Empty(states);
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
    public void PlaybackPosition_SetAtEndOfLastTrack_KeepsPositionAndStaysPaused()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        positions.Clear();

        player.PlaybackPosition = 0.5f;

        Assert.Equal(0.5f, player.PlaybackPosition);
        Assert.Equal([0.5f], positions);
        Assert.Equal(PlaybackStatus.Paused, player.State.Status);
        Assert.Equal(1, mediaEngine.OpenCount);
    }

    [Fact]
    public void TogglePause_SoughtAtEndOfLastTrack_PlaysFromSoughtPosition()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        player.PlaybackPosition = 0.5f;

        player.TogglePause();

        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(50000, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
    }

    [Fact]
    public void SkipBack_AtEndOfLastTrack_MovesFiveSecondsBeforeEndAndStaysPaused()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        positions.Clear();

        player.SkipBack();
        player.TogglePause();

        Assert.Equal(0.95f, positions.Single(), 4);
        Assert.Equal(95000, mediaEngine.OpenedStartTimeMs);
    }

    [Fact]
    public void PlayTrack_SoughtAtEndOfLastTrack_PlaysNewTrackFromStart()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        player.PlaybackPosition = 0.5f;

        player.PlayTrack(AddTrack("/music/two.opus"));

        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.Equal(0, player.PlaybackPosition);
        Assert.Equal(0f, mediaEngine.Position);
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
    public async Task PlayTrack_FileBeingWritten_OpensItFromStartAfterWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        var write = new TaskCompletionSource();
        bool wasOpenedDuringWrite = true;

        var release = player.ReleaseFileAsync("/music/one.opus", async () =>
        {
            await write.Task;
            wasOpenedDuringWrite = mediaEngine.OpenCount > 0;
        });
        player.PlayTrack(item);

        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
        Assert.Same(item, player.State.Track);
        Assert.Equal(0, mediaEngine.OpenCount);

        write.SetResult();
        await release;

        Assert.False(wasOpenedDuringWrite);
        Assert.Equal(1, mediaEngine.OpenCount);
        Assert.Equal("/music/one.opus", mediaEngine.OpenedPath);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
    }

    [Fact]
    public async Task PlaybackPosition_SetWhileFileBeingWritten_OpensItAtSoughtPositionAfterWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        var write = new TaskCompletionSource();
        var release = player.ReleaseFileAsync("/music/one.opus", () => write.Task);
        player.PlayTrack(item);
        positions.Clear();

        player.PlaybackPosition = 0.5f;

        Assert.Equal(0.5f, player.PlaybackPosition);
        Assert.Equal([0.5f], positions);
        Assert.Empty(mediaEngine.SoughtPositions);

        write.SetResult();
        await release;

        Assert.Equal(1, mediaEngine.OpenCount);
        Assert.Equal(50000, mediaEngine.OpenedStartTimeMs);
    }

    [Fact]
    public void PlaybackPosition_SetAfterStopAtEndOfLastTrack_SeeksEngineWithoutReporting()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();
        player.PlaybackPosition = 0.5f;
        player.Stop();
        positions.Clear();

        player.PlaybackPosition = 0.3f;

        Assert.Empty(positions);
        Assert.Equal(0.3f, mediaEngine.Position);
        Assert.Equal(0.3f, player.PlaybackPosition);
    }

    [Fact]
    public async Task PlayTrack_FileBeingWrittenWhileOtherPlays_StopsOtherAndOpensAfterWrite()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/two.opus"));
        var item = AddTrack("/music/one.opus");
        var write = new TaskCompletionSource();

        var release = player.ReleaseFileAsync("/music/one.opus", () => write.Task);
        player.PlayTrack(item);

        Assert.True(mediaEngine.IsStopped);
        Assert.Equal("/music/two.opus", mediaEngine.OpenedPath);

        write.SetResult();
        await release;

        Assert.Equal("/music/one.opus", mediaEngine.OpenedPath);
        Assert.Equal(2, mediaEngine.OpenCount);
    }

    [Fact]
    public async Task PlayTrack_ReleasedTrackPlayedAgain_OpensItFromStartAfterWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        player.PlayTrack(item);
        mediaEngine.Time = 4200;
        int openCountDuringWrite = 0;

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.PlayTrack(item);
            openCountDuringWrite = mediaEngine.OpenCount;
            return Task.CompletedTask;
        });

        Assert.Equal(1, openCountDuringWrite);
        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
    }

    [Fact]
    public async Task PlayTrack_FileBeingWrittenThenPaused_OpensItPausedAfterWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.PlayTrack(item);
            player.TogglePause();
            return Task.CompletedTask;
        });

        Assert.Equal(PlaybackStatus.Paused, player.State.Status);
        Assert.Equal(1, mediaEngine.OpenCount);
        Assert.True(mediaEngine.IsPaused);
    }

    [Fact]
    public async Task PlayTrack_FileBeingWrittenThenStopped_DoesNotOpenAfterWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.PlayTrack(item);
            player.Stop();
            return Task.CompletedTask;
        });

        Assert.Equal(0, mediaEngine.OpenCount);
    }

    [Fact]
    public async Task PlayTrack_FileWrittenTwiceOverlapping_OpensAfterLastWrite()
    {
        var player = CreatePlayer();
        var item = AddTrack("/music/one.opus");
        var firstWrite = new TaskCompletionSource();
        var secondWrite = new TaskCompletionSource();

        var first = player.ReleaseFileAsync("/music/one.opus", () => firstWrite.Task);
        var second = player.ReleaseFileAsync("/music/one.opus", () => secondWrite.Task);
        player.PlayTrack(item);
        firstWrite.SetResult();
        await first;

        Assert.Equal(0, mediaEngine.OpenCount);

        secondWrite.SetResult();
        await second;

        Assert.Equal(1, mediaEngine.OpenCount);
    }

    [Fact]
    public async Task TogglePause_AtEndWhileFileBeingWritten_OpensItAfterWrite()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaiseEndReached();

        await player.ReleaseFileAsync("/music/one.opus", () =>
        {
            player.TogglePause();
            Assert.Equal(1, mediaEngine.OpenCount);
            return Task.CompletedTask;
        });

        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.Equal(PlaybackStatus.Playing, player.State.Status);
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

    [Fact]
    public void PlayTrack_PreviousTrackPositionKept_ReportsStartOnce()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaisePositionChanged(0.7f);
        mediaEngine.Position = 0.7f;
        positions.Clear();

        player.PlayTrack(AddTrack("/music/two.opus"));

        Assert.Equal([0f], positions);
        Assert.Equal(0, player.PlaybackPosition);
    }

    [Fact]
    public void PositionChanged_AfterPlayTrack_ReportsEnginePosition()
    {
        var player = CreatePlayer();
        mediaEngine.Position = 0.7f;
        player.PlayTrack(AddTrack("/music/one.opus"));

        mediaEngine.Position = 0.01f;
        mediaEngine.RaisePositionChanged(0.01f);

        Assert.Equal(0.01f, player.PlaybackPosition);
        Assert.Equal(0.01f, positions.Last());
    }

    [Fact]
    public void PlaybackPosition_SetAfterPlayTrack_ReportsSeekPosition()
    {
        var player = CreatePlayer();
        mediaEngine.Position = 0.7f;
        player.PlayTrack(AddTrack("/music/one.opus"));

        player.PlaybackPosition = 0.3f;

        Assert.Equal(0.3f, player.PlaybackPosition);
    }

    [Fact]
    public void TogglePause_Playing_DoesNotReportPosition()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaisePositionChanged(0.5f);
        mediaEngine.Position = 0.5f;
        positions.Clear();

        player.TogglePause();
        player.TogglePause();

        Assert.Empty(positions);
        Assert.Equal(0.5f, player.PlaybackPosition);
    }

    [Fact]
    public void Skip_Playing_DoesNotReportPosition()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaisePositionChanged(0.5f);
        mediaEngine.Position = 0.5f;
        positions.Clear();

        player.SkipForward();
        player.SkipBack();

        Assert.Empty(positions);
    }

    [Fact]
    public async Task ReleaseFileAsync_Playing_DoesNotReportPosition()
    {
        var player = CreatePlayer();
        player.PlayTrack(AddTrack("/music/one.opus"));
        mediaEngine.RaisePositionChanged(0.5f);
        mediaEngine.Position = 0.5f;
        positions.Clear();

        await player.ReleaseFileAsync("/music/one.opus", () => Task.CompletedTask);

        Assert.Empty(positions);
        Assert.Equal(0.5f, player.PlaybackPosition);
    }

    private AudioPlayer CreatePlayer()
    {
        var player = new AudioPlayer(logger, mediaEngine, volumeStorageMock.Object);
        player.StateChanged += it => states.Add(it);
        player.PlaybackPositionChanged += it => positions.Add(it);

        return player;
    }

    private IPlaylistItem AddTrack(string audioPath)
    {
        playlist.AddTrack(new AudioTrack(audioPath, new Metadata() { Duration = TimeSpan.FromSeconds(100) }));

        return playlist.GetItem(playlist.TrackCount - 1);
    }
}
