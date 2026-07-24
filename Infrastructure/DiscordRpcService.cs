using Core;
using DiscordRPC;

namespace Infrastructure;

public class DiscordRpcService : IRpcService
{
    private readonly DiscordRpcClient client;
    
    private const string AppId = "1494383204252258484";

    public DiscordRpcService()
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
