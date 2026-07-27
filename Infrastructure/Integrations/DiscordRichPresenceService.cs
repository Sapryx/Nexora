using Core.Integrations;
using DiscordRPC;

namespace Infrastructure.Integrations;

public class DiscordRichPresenceService : IRichPresenceService
{
    private readonly DiscordRpcClient client;
    
    private const string AppId = "1494383204252258484"; // TODO Pass from outside

    public DiscordRichPresenceService()
    {
        client = new DiscordRpcClient(AppId);
    }

    public void Initialize()
    {
        client.Initialize();
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

    public void Dispose()
    {
        client.Dispose();
    }
}
