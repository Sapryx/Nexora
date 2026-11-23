using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Core.Integrations;
using Core.Playback;
using Core.Playlists;
using Core.Tracks;
using Microsoft.Extensions.DependencyInjection;
using Nexora.Controls;
using Nexora.ViewModels;
using Nexora.Views;
using Tests.Shared.Integrations;
using Tests.Shared.Playback;

namespace Nexora.UiTests.Views;

public class DiscordReconnectTests
{
    private readonly FakeMediaEngine mediaEngine = new FakeMediaEngine();

    [AvaloniaFact]
    public void Presence_DiscordConnectsAfterTrackStarted_ShowsCurrentTrack()
    {
        using var app = new TestMainWindow(mediaEngine);
        var discord = app.DiscordPipe;
        var queue = app.Services.GetRequiredService<PlaybackQueue>();
        var audioPlayer = app.Services.GetRequiredService<IAudioPlayer>();
        app.Services.GetRequiredService<IRichPresenceService>().Initialize();
        queue.AddTrack(CreateTrack("/music/psycho.opus", "Psycho", "Muse"));
        Dispatcher.UIThread.RunJobs();
        audioPlayer.PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();
        Assert.True(app.Services.GetRequiredService<PlaybackVm>().IsPlaying);

        discord.Start();
        AssertShownAfterConnect(discord, "Psycho", "Muse");

        int refusedConnections = discord.RefusedConnections;
        discord.Quit();
        discord.WaitUntil(() => discord.RefusedConnections > refusedConnections);
        Assert.Null(discord.Activity);
        discord.Start();
        AssertShownAfterConnect(discord, "Psycho", "Muse");

        discord.Reload();
        AssertShownAfterConnect(discord, "Psycho", "Muse");

        app.Click(app.FindButton("pause", app.Find<PlaybackControl>()));
        Assert.False(app.Services.GetRequiredService<PlaybackVm>().IsPlaying);
        discord.Reload();
        AssertShownAfterConnect(discord, "Psycho", "Muse");

        QueueTileOf(app).RemoveFromQueueCommand.Execute(null);
        Dispatcher.UIThread.RunJobs();
        Assert.Null(audioPlayer.State.Track);
        discord.Reload();
        discord.WaitUntil(() => discord.ReceivedActivity);
        Assert.Null(discord.Activity);

        queue.AddTrack(CreateTrack("/music/uprising.opus", "Uprising", "Muse"));
        Dispatcher.UIThread.RunJobs();
        audioPlayer.PlayTrack(queue.GetItem(0));
        Dispatcher.UIThread.RunJobs();
        discord.WaitUntil(() => discord.Activity?.Details == "Uprising");
        Assert.Equal("Muse", discord.Activity?.State);
    }

    private static AudioTrack CreateTrack(string path, string title, string artist)
    {
        var metadata = new Metadata() { Title = title, Artists = [artist], Duration = TimeSpan.FromSeconds(100) };

        return new AudioTrack(path, metadata);
    }

    private static void AssertShownAfterConnect(FakeDiscordPipe discord, string title, string artist)
    {
        discord.WaitUntil(() => discord.ReceivedActivity);
        Assert.Equal(title, discord.Activity?.Details);
        Assert.Equal(artist, discord.Activity?.State);
    }

    private static TrackControlVm QueueTileOf(TestMainWindow app)
    {
        return app.Window.GetVisualDescendants().OfType<TrackControl>()
            .Select(control => control.DataContext).OfType<TrackControlVm>().Single();
    }
}
