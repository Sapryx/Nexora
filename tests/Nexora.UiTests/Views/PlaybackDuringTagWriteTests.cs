using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Core.Library;
using Core.Playback;
using Core.Playlists;
using Core.Storage.Tags;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.ViewModels;
using Tests.Shared.Playback;
using Tests.Shared.Storage;

namespace Nexora.UiTests.Views;

public sealed class PlaybackDuringTagWriteTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"nexora-ui-write-{Guid.NewGuid()}");
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();
    private readonly GatedMetadataSaver saver;

    public PlaybackDuringTagWriteTests()
    {
        Directory.CreateDirectory(directory);
        saver = new GatedMetadataSaver(new TagLibMetadataSaver(), mediaEngine);
    }

    [AvaloniaFact]
    public void PlayTrack_WhileItsTagsAreWritten_StartsAfterWriteAndWriteSucceeds()
    {
        using var app = new TestMainWindow(mediaEngine, saver);
        var queue = app.Services.GetRequiredService<PlaybackQueue>();
        var player = app.Services.GetRequiredService<IAudioPlayer>();
        var tagWriter = app.Services.GetRequiredService<TrackTagWriter>();
        var playbackVm = app.Services.GetRequiredService<PlaybackVm>();
        string path = TestWavFile.Create(directory);
        var track = new AudioTrack(path, new Metadata() { Duration = TimeSpan.FromSeconds(100) });
        queue.AddTrack(track);
        Dispatcher.UIThread.RunJobs();

        var edit = tagWriter.EditAsync(track, "New title", ["New artist"]);
        saver.WaitUntilStarted();
        player.PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();

        Assert.True(playbackVm.IsPlaying);
        Assert.Equal(0, mediaEngine.OpenCount);

        saver.Finish();
        Assert.True(edit.Wait(Timeout));
        Assert.True(edit.Result);
        Dispatcher.UIThread.RunJobs();

        using(var tagFile = TagLib.File.Create(path))
        {
            Assert.Equal("New title", tagFile.Tag.Title);
            Assert.Equal(["New artist"], tagFile.Tag.Performers);
        }

        Assert.Equal(1, mediaEngine.OpenCount);
        Assert.Equal(path, mediaEngine.OpenedPath);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
        Assert.True(playbackVm.IsPlaying);

        mediaEngine.Time = 30000;
        mediaEngine.RaisePositionChanged(0.3f);
        Dispatcher.UIThread.RunJobs();
        var save = Task.Run(() => tagWriter.SaveLoudnessAsync(track, new TrackLoudness() { Lufs = -9.25 }));
        saver.WaitUntilStarted();
        Assert.False(mediaEngine.HoldsFile);

        player.PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, mediaEngine.OpenCount);

        saver.Finish();
        Assert.True(save.Wait(Timeout));
        Dispatcher.UIThread.RunJobs();

        using(var tagFile = TagLib.File.Create(path))
        {
            Assert.Equal(-9.25, LoudnessTags.Read(tagFile)?.Lufs ?? 0, 6);
            Assert.Equal("New title", tagFile.Tag.Title);
        }

        Assert.Equal(2, mediaEngine.OpenCount);
        Assert.Equal(0, mediaEngine.OpenedStartTimeMs);
        Assert.False(mediaEngine.IsPaused);
        Assert.True(playbackVm.IsPlaying);
    }

    public void Dispose()
    {
        saver.Dispose();
        Directory.Delete(directory, true);
    }
}
