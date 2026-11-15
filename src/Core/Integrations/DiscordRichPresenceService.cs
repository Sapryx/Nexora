using Core.Logging;
using Core.Playback;
using DiscordRPC;
using Microsoft.Extensions.Logging;

namespace Core.Integrations;

public class DiscordRichPresenceService : IRichPresenceService
{
    private const int MaxTextLength = 128;
    private const string Ellipsis = "…";
    private readonly ILogger<DiscordRichPresenceService> logger;
    private readonly IDiscordClient client;
    private bool connectionFailureLogged;

    public DiscordRichPresenceService(
        IAudioPlayer audioPlayer,
        IDiscordClient client,
        ILogger<DiscordRichPresenceService> logger)
    {
        this.client = client;
        this.logger = logger;

        client.Ready += OnReady;
        client.ConnectionFailed += OnConnectionFailed;
        client.Error += (code, message) => logger.Warn($"(Discord) Error {code}: {message}");
        audioPlayer.StateChanged += OnStateChanged;
    }

    public void Initialize()
    {
        client.Initialize();
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

    private void OnStateChanged(PlaybackState state)
    {
        if(state.Track == null)
        {
            client.SetPresence(null);
            return;
        }

        var metadata = state.Track.AudioTrack.Metadata;
        UpdateStatus(metadata.Title, metadata.JoinedArtists);
    }
}
