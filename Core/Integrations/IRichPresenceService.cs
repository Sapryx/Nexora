namespace Core.Integrations;

public interface IRichPresenceService : IDisposable
{
    public void Initialize();
    public void UpdateStatus(string title, string artist);
}
