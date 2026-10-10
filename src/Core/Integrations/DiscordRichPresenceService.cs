using Core.Playback;
using DiscordRPC;
using DiscordRPC.Logging;

namespace Core.Integrations;

public class DiscordRichPresenceService : IRichPresenceService
{
    private const string AppId = "1494383204252258484"; // TODO Pass from outside
    private const int MaxTextLength = 128;
    private const string Ellipsis = "…";
    private readonly IAudioPlayer audioPlayer;
    private readonly DiscordRpcClient client;

    public DiscordRichPresenceService(IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
        client = new DiscordRpcClient(AppId);
        client.Logger = new FileLogger($"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Nexora/logs/discord.log");

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
        client.SetPresence(CreatePresence(title, artist));
    }

    public static RichPresence CreatePresence(string title, string artist)
    {
        return new RichPresence()
        {
            Details = Truncate(title),
            State = Truncate(artist),
            Type = ActivityType.Listening
        };
    }

    private static string Truncate(string text)
    {
        if(text.Length <= MaxTextLength)
        {
            return text;
        }

        int length = MaxTextLength - Ellipsis.Length;

        if(char.IsHighSurrogate(text[length - 1]))
        {
            length--;
        }

        return text.Substring(0, length) + Ellipsis;
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
