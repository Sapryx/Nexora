using Core.Storage.Tags;
using Core.Tracks;
using Tests.Shared.Playback;

namespace Nexora.UiTests;

public sealed class GatedMetadataSaver : IMetadataSaver, IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly IMetadataSaver saver;
    private readonly FakeMediaEngine mediaEngine;
    private readonly SemaphoreSlim started = new SemaphoreSlim(0);
    private readonly SemaphoreSlim finishing = new SemaphoreSlim(0);

    public GatedMetadataSaver(IMetadataSaver saver, FakeMediaEngine mediaEngine)
    {
        this.saver = saver;
        this.mediaEngine = mediaEngine;
    }

    public void Save(MetadataChange change)
    {
        Write(change.AudioPath, () => saver.Save(change));
    }

    public void SaveLoudness(string audioPath, TrackLoudness loudness)
    {
        Write(audioPath, () => saver.SaveLoudness(audioPath, loudness));
    }

    public void WaitUntilStarted()
    {
        Assert.True(started.Wait(Timeout), "The write did not start");
    }

    public void Finish()
    {
        finishing.Release();
    }

    public void Dispose()
    {
        started.Dispose();
        finishing.Dispose();
    }

    private void Write(string audioPath, Action write)
    {
        started.Release();
        Assert.True(finishing.Wait(Timeout), "The write was not finished");

        if(mediaEngine.HoldsFile && mediaEngine.OpenedPath == audioPath)
        {
            throw new IOException($"{audioPath} is opened by the player");
        }

        write();
    }
}
