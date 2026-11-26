using Core.Logging;
using DiscordRPC;
using DiscordRPC.IO;
using Microsoft.Extensions.Logging;

namespace Core.Integrations;

public class DiscordRpcClientAdapter : IDiscordClient
{
    private const string AppId = "1494383204252258484";
    private readonly DiscordRpcClient client;

    public event Action<string>? Ready;
    public event Action? ConnectionFailed;
    public event Action<int, string>? Error;

    public DiscordRpcClientAdapter(INamedPipeClient pipeClient, ILogger<DiscordRpcLogger> rpcLogger)
    {
        client = new DiscordRpcClient(AppId, client: pipeClient)
        {
            Logger = new DiscordRpcLogger(rpcLogger),
            SkipIdenticalPresence = false
        };

        client.OnReady += (_, e) => Ready?.Invoke(e.User.Username);
        client.OnConnectionFailed += (_, _) => ConnectionFailed?.Invoke();
        client.OnError += (_, e) => Error?.Invoke((int)e.Code, e.Message);
    }

    public void Initialize()
    {
        client.Initialize();
    }

    public void SetPresence(RichPresence? presence)
    {
        client.SetPresence(presence);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
