using Core.Logging;
using Core.Playback;
using DiscordRPC;
using Microsoft.Extensions.Logging;

namespace Core.Integrations;

public class DiscordRichPresenceService : IRichPresenceService
{
    private const string AppId = "1494383204252258484"; // TODO Pass from outside
    private const int MaxTextLength = 128;
    private const string Ellipsis = "…";
    private readonly IAudioPlayer audioPlayer;
    private readonly ILogger<DiscordRichPresenceService> logger;
    private readonly DiscordRpcClient client;
    private bool connectionFailureLogged;

    public DiscordRichPresenceService(
        IAudioPlayer audioPlayer,
        ILogger<DiscordRichPresenceService> logger,
        ILogger<DiscordRpcLogger> rpcLogger)
    {
        this.audioPlayer = audioPlayer;
        this.logger = logger;
        client = new DiscordRpcClient(AppId)
        {
            Logger = new DiscordRpcLogger(rpcLogger)
        };

        client.OnReady += (_, e) => OnReady(e.User.Username);
        client.OnConnectionFailed += (_, _) => OnConnectionFailed();
        client.OnError += (_, e) => logger.Warn($"(Discord) Error {e.Code}: {e.Message}");
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

    public void OnReady(string username)
    {
        connectionFailureLogged = false;
        logger.Info($"(Discord) Connected as {username}");
    }

    public void OnConnectionFailed()
    {
        if(!connectionFailureLogged)
        {
            connectionFailureLogged = true;
            logger.Info($"(Discord) Discord is not running, rich presence will start when it connects");
        }
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
