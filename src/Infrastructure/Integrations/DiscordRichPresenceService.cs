using Core.Integrations;
using Core.Playback;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Infrastructure.Integrations;

public class DiscordRichPresenceService : IRichPresenceService
{
    private readonly IAudioPlayer audioPlayer;
    private readonly DiscordRpcClient client;
    
    private const string AppId = "1494383204252258484"; // TODO Pass from outside

    public DiscordRichPresenceService(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
        client = new DiscordRpcClient(AppId);
        client.Logger = new FileLogger($"{AppContext.BaseDirectory}/logs/discord.log");

        audioPlayer.PlaybackStarted += OnPlaybackStarted;
    }

    public void Initialize()
    {
        client.Initialize();
    }

    public void Dispose()
    {
        client.Dispose();
    }

    public void UpdateStatus(string title, string artist)
    {
        var richPresence = new RichPresence
        {
            Details = title,
            State = artist,
            Type = ActivityType.Listening
        };
        
        client.SetPresence(richPresence);
    }

    private void OnPlaybackStarted()
    {
        var playlistItem = audioPlayer.NowPlaying;

        if(playlistItem != null)
        {
            string title = playlistItem.AudioTrack.Metadata.Title;
            string artists = playlistItem.AudioTrack.Metadata.Artists;
            UpdateStatus(title, artists);
        }
    }
}
