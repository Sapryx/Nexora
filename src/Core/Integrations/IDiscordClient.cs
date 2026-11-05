using DiscordRPC;

namespace Core.Integrations;

public interface IDiscordClient : IDisposable
{
    public event Action<string>? Ready;
    public event Action? ConnectionFailed;
    public event Action<int, string>? Error;

    public void Initialize();
    public void SetPresence(RichPresence? presence);
}
