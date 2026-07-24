namespace Core;

public interface IRpcService : IDisposable
{
    public void Initialize();
    public void UpdateStatus(string title, string artist);
}
